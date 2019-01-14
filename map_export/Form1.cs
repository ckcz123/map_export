using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using map_export.Properties;

namespace map_export
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = Settings.Default.rmPath;
            textBox2.Text = Settings.Default.h5Path;
            setConfigChecked();

            ToolTip tip = new ToolTip()
            {
                AutoPopDelay = 10000,
                InitialDelay = 500,
                ReshowDelay = 500,
                ShowAlways = true,
                UseAnimation = true,
                UseFading = true
            };

            tip.SetToolTip(pictureBox1, "是否自动将标准20*15尺寸的地图剪切成适合H5的13*13大小。");
            tip.SetToolTip(pictureBox2, "当RM塔的地图名并非\"1001\"这样的格式时，需要人工指定转换开始的初始地图。\n" +
                                        "这里的数字对应的是RM地图设置的ID:xxx。\n" +
                                        "你可能需要在强制转换完毕后再手动调整H5的地图顺序。");
            tip.SetToolTip(pictureBox3, "勾选后会将所有音频文件也一并转移，游戏可能会变得非常大。");
            tip.SetToolTip(pictureBox4, "仅需在第一次转换塔时勾选此项，会生成一个config.json配置文件。\n可按需对该配置文件进行自定义修改后再重新进行导出。");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            dialog.SelectedPath = textBox1.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = dialog.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = textBox2.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = dialog.SelectedPath;
                setConfigChecked();
            }
        }

        private void setConfigChecked()
        {
            if (Directory.Exists(textBox2.Text))
            {
                checkBox4.Checked = !File.Exists(textBox2.Text + "\\config.json");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string rmPath = textBox1.Text, h5Path = textBox2.Text;
            // Check if valid
            if (!(Directory.Exists(rmPath) && Directory.Exists(rmPath + "\\Data") &&
                Directory.Exists(rmPath + "\\Graphics")))
            {
                MessageBox.Show("不是有效的RM目录！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox1.Focus();
                return;
            }

            if (!(Directory.Exists(h5Path) && Directory.Exists(h5Path + "\\libs") &&
                  Directory.Exists(h5Path + "\\project") && File.Exists(h5Path + "\\main.js")))
            {
                MessageBox.Show("不是有效的H5目录！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox2.Focus();
                return;
            }

            // 检查重复使用
            if (File.Exists(h5Path + "\\error.log"))
            {
                MessageBox.Show("你已经对该H5样板进行过转换操作，请重新选择一个崭新的H5样板。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox2.Focus();
                return;
            }

            // 检查是否存在 __VERSION__
            string text = File.ReadAllText(h5Path + "\\main.js");
            if (!text.Contains("__VERSION__"))
            {
                MessageBox.Show("该版本的H5样板不受支持，请使用最新样板！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox2.Focus();
                return;
            }

            if (!File.Exists("RGSS2H5.exe"))
            {
                MessageBox.Show("RGSS2H5.exe不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox2.Focus();
                return;
            }

            string arguments = "\"" + rmPath + "\" \"" + h5Path + "\"";
            if (checkBox1.Checked) arguments += " -c";
            if (checkBox2.Checked)
            {
                arguments += " -f " + numericUpDown1.Text;
            }
            if (checkBox3.Checked) arguments += " -a";
            if (checkBox4.Checked) arguments += " -o";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "RGSS2H5.exe",
                    Arguments = arguments
                }
            };
            process.Start();
            process.WaitForExit();

            switch (process.ExitCode)
            {
                case 0:
                {
                    if (!File.Exists("bitmapWorks.log"))
                    {
                        MessageBox.Show("bitmapWorks.log不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                    string[] lines = File.ReadAllLines("bitmapWorks.log", Encoding.Default);
                    File.Delete("bitmapWorks.log");
                    string message = new ImageOperation(lines, rmPath + "\\", h5Path + "\\error.log").work();
                    if (message != null)
                    {
                        MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                    MessageBox.Show("部分地图或事件可能没有成功进行转换，日志已写入H5样板目录下的 error.log，请注意查看。",
                        "导出成功！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                break;
                case 1: MessageBox.Show("参数错误！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); break;
                case 2: MessageBox.Show("rgss.dll初始化失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); break;
                case 3: MessageBox.Show("H5样板信息处理错误！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); break;
                case 4: MessageBox.Show("成功在目标样板中仅生成配置文件config.json，请根据需要修改配置文件后再进行生成。", 
                    "配置文件生成成功！", MessageBoxButtons.OK, MessageBoxIcon.Information); break;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Enabled = checkBox2.Checked;
            if (checkBox2.Checked) numericUpDown1.Focus();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            File.Delete("bitmapWorks.log");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.rmPath = textBox1.Text;
            Settings.Default.h5Path = textBox2.Text;
            Settings.Default.Save();
        }

    }
}
