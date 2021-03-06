﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using Xceed.Document.NET;

namespace Gaussian_Quick_Output
{
    public partial class Form1 : Form
    {
        public CustomFunctions customFunctions;
        BindingSource bindingSource;
        public static string dataset = "";
        public static DataTable valueTable = new DataTable();
        public static string fullreport = "";
        public static List<string> fileList = new List<string>();

        public Form1()
        {
            InitializeComponent();
            customFunctions = new CustomFunctions();
            bindingSource = new BindingSource();
            bindingSource.DataSource = customFunctions.FunctionList;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChooseFolder();
        }

        //Function to preview data of log files
        //Some data validation would be nice, but not completely necessary
        public void dataLookup()
        {
            if (listBox1.SelectedIndex != -1 && comboBox1.SelectedIndex != -1)
            {

                try
                {
                    //Adding support for microsoft word
                    if (listBox1.Text.EndsWith(".docx"))
                    {
                        using (var document = Xceed.Words.NET.DocX.Load(listBox1.Text))
                        {
                            string file = document.Text;
                            CustomFunction c = (CustomFunction)comboBox1.SelectedItem;
                            textBox1.Text = c.ReadFunction(file);
                        }
                    }
                    else
                    {
                        string file = System.IO.File.ReadAllText(listBox1.Text);
                        CustomFunction c = (CustomFunction)comboBox1.SelectedItem;
                        textBox1.Text = c.ReadFunction(file);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);

                }

            }
            else
            {
                textBox1.Text = "File not found";
            }

        }

        public void processData(IProgress<int> progress)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("File Name");
            foreach (CustomFunction c in customFunctions.FunctionList)
            {
                dt.Columns.Add(c.Name);
            }
            int i = 0;
            foreach (string filepath in fileList)
            {
                string file = System.IO.File.ReadAllText(filepath);
                List<String> row = new List<string>();
                string fpath = filepath.Substring(filepath.LastIndexOf(@"\") + 1);
                row.Add(fpath);
                foreach (CustomFunction c in customFunctions.FunctionList)
                {
                    row.Add(c.ReadFunction(file));
                }
                dt.Rows.Add(row.ToArray());
                i++;
                var percentComplete = (i * 100) / fileList.Count;
                progress.Report(percentComplete);
            }
            valueTable = null;
            valueTable = dt;
        }


        public void ChooseFolder()
        {
            fileList.Clear();
            //A little bit of QoL code to set the default file to be the most recent file selected. 
            //Reduces the headache of navigating through hella directories just to find your folder
            //Just a little code snippet from Stackoverflow
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (!String.IsNullOrEmpty(Properties.Settings.Default.lastDirectory))
                {
                    Properties.Settings.Default.lastDirectory = folderBrowserDialog1.SelectedPath;
                }
                Properties.Settings.Default.lastDirectory = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.Save();

                //Encapsulated in a try/catch block so program doesnt die because of any exception
                //Simply populates the list with all log files
                listBox1.Items.Clear();
                try
                {
                    folderBrowserDialog1.SelectedPath = Properties.Settings.Default.lastDirectory;
                    foreach (string file in Directory.GetFiles(folderBrowserDialog1.SelectedPath))
                    {
                        //Not sure if there's a smarter way to do this but this seems pretty fool-proof
                        if (file.EndsWith(Properties.Settings.Default.fileTypeSearch))
                        {
                            listBox1.Items.Add(file);
                            fileList.Add(file);
                        }
                    }
                }
                catch (Exception f)
                {
                    MessageBox.Show(f.ToString());
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataLookup();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
        }

        //Method saves everything by processing the data and opening up file system dialog. 
        //Can be expanded upon in the future in case further customization is needed, but right now this is perfectly fine
        private async void button2_Click(object sender, EventArgs e)
        {
            progressBar1.Visible = true;
            var progress = new Progress<int>(value =>
            {
                progressBar1.Value = value;


            });
            await Task.Run(() => processData(progress));
            progressBar1.Visible = false;

            var lines = new List<string>();
            string[] columnNames = valueTable.Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName)
                .ToArray();

            var header = string.Join(",", columnNames.Select(name => $"\"{name}\""));
            lines.Add(header);

            var valueLines = valueTable.AsEnumerable()
           .Select(row => string.Join(",", row.ItemArray.Select(val => $"\"{val}\"")));

            lines.AddRange(valueLines);


            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllLines(saveFileDialog1.FileName, lines);

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(Properties.Settings.Default.lastDirectory))
            {
                folderBrowserDialog1.SelectedPath = Properties.Settings.Default.lastDirectory;
            }
            if (!String.IsNullOrEmpty(Properties.Settings.Default.currentXMLTemplate))
            {
                string filePath = Properties.Settings.Default.currentXMLTemplate;

                //Read the contents of the file into a stream
                try
                {
                    var fileStream = openFileDialog1.OpenFile();
                }
                catch (Exception er)
                {
                    er.ToString();
                }
                XmlSerializer ser = new XmlSerializer(typeof(CustomFunctions), new Type[] { typeof(AbsoluteSearchFunction), typeof(StringOccurenceFunction) });
                try
                {
                    StreamReader rdr = new StreamReader(filePath);
                    customFunctions = (CustomFunctions)ser.Deserialize(rdr);
                    updateBindings();
                }

                catch (Exception er)
                {
                    MessageBox.Show(er.Message);
                }

            }

            dataset = "";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != -1)
            {
                CustomFunction c = (CustomFunction)comboBox1.SelectedItem;
                textBox1.Text = c.ReadFunction("sadfasdfasdfasdf");
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            progressBar1.Visible = true;
            var progress = new Progress<int>(value =>
            {
                progressBar1.Value = value;


            });
            await Task.Run(() => processData(progress));
            progressBar1.Visible = false;
            Form2 form = new Form2();
            form.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form3 form = new Form3();
            form.Show();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            Form3 form = new Form3();
            form.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                if (listBox1.SelectedItem.ToString().EndsWith(".com") && Properties.Settings.Default.ComFileProgram)
                {
                    System.Diagnostics.Process.Start("notepad.exe", listBox1.SelectedItem.ToString());
                }
                else
                {
                    System.Diagnostics.Process.Start(listBox1.SelectedItem.ToString());
                }
            }
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void button7_Click(object sender, EventArgs e)
        {

        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form5 form = new Form5();
            form.Show();
        }

        private void editAutomationTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form6 form = new Form6(false);
            if (form.ShowDialog() == DialogResult.OK)
            {
                customFunctions = form.SessionTemplate;
                updateBindings();
            }
        }

        private void newAutomationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form6 form = new Form6(true);
            if (form.ShowDialog() == DialogResult.OK)
            {
                customFunctions = form.SessionTemplate;
                updateBindings();
            }

        }

        private void findAndReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form4 form = new Form4();
            form.Show();
        }

        private async void saveResultsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            progressBar1.Visible = true;
            var progress = new Progress<int>(value =>
            {
                progressBar1.Value = value;


            });
            await Task.Run(() => processData(progress));
            progressBar1.Visible = false;

            var lines = new List<string>();
            string[] columnNames = valueTable.Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName)
                .ToArray();

            var header = string.Join(",", columnNames.Select(name => $"\"{name}\""));
            lines.Add(header);

            var valueLines = valueTable.AsEnumerable()
           .Select(row => string.Join(",", row.ItemArray.Select(val => $"\"{val}\"")));

            lines.AddRange(valueLines);


            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllLines(saveFileDialog1.FileName, lines);

            }
        }

        private void viewDatasetInGridToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Form2 form = new Form2();
            form.Show();
        }

        private void openAutomationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                string filePath = openFileDialog1.FileName;
                Properties.Settings.Default.currentXMLTemplate = filePath;
                MessageBox.Show(Properties.Settings.Default.currentXMLTemplate);


                //Read the contents of the file into a stream
                var fileStream = openFileDialog1.OpenFile();
                XmlSerializer ser = new XmlSerializer(typeof(CustomFunctions), new Type[] { typeof(AbsoluteSearchFunction), typeof(StringOccurenceFunction) });
                try
                {
                    StreamReader rdr = new StreamReader(filePath);
                    customFunctions = (CustomFunctions)ser.Deserialize(rdr);


                    updateBindings();
                }
                catch (Exception er)
                {
                    er.ToString();
                }


            }
        }
        private void updateBindings()
        {
            bindingSource.DataSource = null;
            bindingSource.DataSource = customFunctions.FunctionList;
            checkedListBox1.DataSource = null;
            checkedListBox1.DataSource = bindingSource.DataSource;
            comboBox1.DataSource = null;
            comboBox1.DataSource = bindingSource.DataSource;


            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "Name";
            checkedListBox1.DataSource = bindingSource.DataSource;
            checkedListBox1.DisplayMember = "Name";
            checkedListBox1.ValueMember = "Name";

        }

        private void button4_Click_2(object sender, EventArgs e)
        {
            System.Reflection.MethodInfo meth = typeof(StringOccurenceFunction).GetMethod("Create");
            customFunctions.FunctionList.Add(CustomFunction.Build(meth));
            bindingSource.DataSource = null;

            bindingSource.DataSource = customFunctions.FunctionList;



        }
    }
}
