using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace map_export
{
    class ImageOperation
    {
        private string[] operations;
        private Dictionary<string, Bitmap> dict;
        private HashSet<string> modified;

        private int mode = 0; // 0-move; 1-blt; 2-clear

        private string src = "";

        private string directory;

        private string log;

        public ImageOperation(string[] ops, string workDirectory, string logPath)
        {
            operations = ops;
            directory = workDirectory;
            log = logPath;
            dict = new Dictionary<string, Bitmap>();
            modified = new HashSet<string>();

            File.AppendAllLines(log, new []{"","-------------",""},Encoding.Default);
        }

        public string work()
        {
            foreach (string operation in operations)
            {
                string[] items = operation.Trim().Split('\t');
                if (items.Length == 0 || items[0].Equals("")) continue;

                if (items[0].Equals("move:"))
                {
                    mode = 0;
                    continue;                    
                }

                if (items[0].Equals("blt:"))
                {
                    mode = 1;
                    continue;
                }

                if (items[0].Equals("clear:"))
                {
                    mode = 2;
                    continue;
                }

                if (mode == 0)
                {
                    if (items.Length == 2 && move(directory+items[0], items[1]))
                        continue;
                    File.AppendAllText(log, "不合法的复制操作：" + operation + "\n", Encoding.Default);
                    return "不合法的复制操作：" + operation;
                }

                if (mode == 1)
                {
                    if (items.Length == 1)
                    {
                        src = directory + items[0];
                        if (cacheImage(src)) continue;
                        File.AppendAllText(log, "无法缓存图片：" + operation + "\n", Encoding.Default);
                        return "无法缓存图片：" + operation;
                    }

                    if (items.Length == 9 &&
                        blt(items[0], stoi(items[1]), stoi(items[2]), stoi(items[3]), stoi(items[4]),
                            stoi(items[5]), stoi(items[6]), stoi(items[7]), stoi(items[8])))
                        continue;
                    File.AppendAllText(log, "不合法的剪切操作：" + operation + "\n", Encoding.Default);
                    return "不合法的剪切操作：" + operation;
                }

                if (mode == 2)
                {
                    if (items.Length == 1)
                    {
                        src = items[0];
                        if (cacheImage(src)) continue;
                        File.AppendAllText(log, "无法缓存图片：" + operation + "\n", Encoding.Default);
                        return "无法缓存图片：" + operation;
                    }

                    if (items.Length == 4 && clear(stoi(items[0]), stoi(items[1]), stoi(items[2]), stoi(items[3])))
                        continue;
                    File.AppendAllText(log, "不合法的清除操作：" + operation + "\n", Encoding.Default);
                    return "不合法的清除操作：" + operation;
                }

            }

            return saveAllImages();
        }

        private bool move(string src, string target)
        {
            try
            {
                // 检测src是不是合法的文件
                if (!src.ToLower().EndsWith(".jpg") && !src.ToLower().EndsWith(".png"))
                {
                    File.AppendAllText(log, "不合法的图片文件："+src+"\n", Encoding.Default);
                    Bitmap bitmap = new Bitmap(32,32);
                    bitmap.Save(target);
                    bitmap.Dispose();
                    return true;
                }

                File.Copy(src, target, true);
                return true;
            }
            catch (Exception e)
            {
                File.AppendAllText(log, e.StackTrace+"\n", Encoding.Default);
                return false;
            }
        }

        private bool blt(string target, int sx, int sy, int sw, int sh, int dx, int dy, int hue,
            int opacity)
        {
            if (!cacheImage(target)) return false;
            Bitmap smap = dict[src], tmap = dict[target];
            Bitmap tmpBitmap;
            if (tmap == null)
            {
                File.AppendAllText(log, "无效的图片文件："+target+"\n", Encoding.Default);
                return false;
            }
            if (smap != null)
            {
                tmpBitmap = Util.setOpacity(dict[src], sx, sy, sw, sh, opacity);
                if (hue > 0)
                    Util.addHue(tmpBitmap, hue);
            }
            else
            {
                tmpBitmap = new Bitmap(sw, sh);
            }

            if (tmap.Height < dy + sh)
            {
                Bitmap another = new Bitmap(tmap.Width, dy+sh);
                Graphics graphics = Graphics.FromImage(another);
                Util.drawImage(graphics, tmap);
                graphics.Dispose();
                tmap.Dispose();
                tmap = another;
            }
           
            Graphics g = Graphics.FromImage(tmap);
            g.Clip = new Region(new Rectangle(dx, dy, sw, sh));
            g.Clear(Color.Transparent);
            Util.drawImage(g, tmpBitmap, dx, dy);
            g.Dispose();
            tmpBitmap.Dispose();
            
            dict[target] = tmap;
            modified.Add(target);
            return true;
        }

        private bool clear(int sx, int sy, int sw, int sh)
        {
            if (!cacheImage(src)) return false;
            Bitmap tmap = dict[src];
            if (tmap == null)
            {
                File.AppendAllText(log, "无效的图片文件：" + src + "\n", Encoding.Default);
                return false;
            }
            Graphics g = Graphics.FromImage(tmap);
            g.Clip = new Region(new Rectangle(sx, sy, sw, sh));
            g.Clear(Color.Transparent);
            g.Dispose();
            modified.Add(src);
            return true;
        }

        private bool cacheImage(string src)
        {
            // 检测src是否存在
            if (dict.ContainsKey(src)) return true;
            // 检测无后缀名，直接忽略
            if (!src.ToLower().EndsWith(".png") && !src.ToLower().EndsWith(".jpg"))
            {
                File.AppendAllText(log, "无后缀名文件："+src+"\n", Encoding.Default);
                dict.Add(src, null);
                return true;
            }
            try
            {
                Bitmap bitmap = (Bitmap) Image.FromFile(src);
                Bitmap mp = new Bitmap(bitmap.Width, bitmap.Height);
                Graphics g = Graphics.FromImage(mp);
                Util.drawImage(g, bitmap);
                g.Dispose();
                bitmap.Dispose();
                dict.Add(src, mp);
                return true;
            }
            catch (Exception e)
            {
                File.AppendAllText(log, e.StackTrace + "\n", Encoding.Default);
                return false;
            }
        }

        private string saveAllImages()
        {
            foreach (var pair in dict)
            {
                if (!modified.Contains(pair.Key)) continue;
                try
                {
                    pair.Value.Save(pair.Key);
                }
                catch (Exception e)
                {
                    File.AppendAllText(log, e.StackTrace + "\n", Encoding.Default);
                    return e.Message;
                }
            }
            return null;
        }

        private int stoi(string s)
        {
            return Convert.ToInt32(s); 
        }
        
    }
}
