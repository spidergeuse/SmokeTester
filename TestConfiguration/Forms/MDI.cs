﻿/**
* Smoke Tester Tool : Post deployment smoke testing tool.
* 
* http://www.stephenhaunts.com
* 
* This file is part of Smoke Tester Tool.
* 
* Smoke Tester Tool is free software: you can redistribute it and/or modify it under the terms of the
* GNU General Public License as published by the Free Software Foundation, either version 2 of the
* License, or (at your option) any later version.
* 
* Smoke Tester Tool is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
* without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
* 
* See the GNU General Public License for more details <http://www.gnu.org/licenses/>.
* 
* Curator: Stephen Haunts
*/
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Common.Xml;
using ConfigurationTests;

namespace TestConfiguration.Forms
{
    public partial class Mdi : Form
    {
        private bool _closingFlag;

        public Mdi()
        {
            InitializeComponent();
        }         
       
        private static ConfigurationTestSuite GetConfigurationSuiteFromFile(string fileName)
        {
            ConfigurationTestSuite configurationTestSuite = null;

            try
            {
                string xml = File.ReadAllText(fileName,Encoding.Unicode);
                configurationTestSuite = xml.ToObject<ConfigurationTestSuite>();
            }
            catch
            {
                MessageBox.Show(@"Unable to open file. Please check file format is Unicode.", @"Error While Loading File");
            }

            return configurationTestSuite;
        } 

        private void LaunchNewTestEditor(bool loadTemplates)
        {
            var editor = new TestEditor(loadTemplates) { MdiParent = this };
            editor.Show();
        }  
      
        private void LaunchNewTestEditor(ConfigurationTestSuite testSuite, string fileName)
        {
            if (testSuite == null) return;

            testSuite.FileName = fileName;

            var editor = new TestEditor(testSuite) { MdiParent = this };
            editor.Show();
        }

        private void LaunchNewTestEditor(ConfigurationTestSuite testSuite)
        {
            if (testSuite == null) return;            

            var editor = new TestEditor(testSuite) { MdiParent = this };
            editor.Show();
        }  
    
        private void OpenConfigurationFile()
        {
            using (var dialog = new OpenFileDialog { Filter = @"XML Configuration File |*.xml" })
            {
                dialog.FileOk += (s, ce) =>
                {
                    if (ce.Cancel)
                    {
                        return;                        
                    }

                    var testSuite = GetConfigurationSuiteFromFile(dialog.FileName);
                    LaunchNewTestEditor(testSuite, dialog.FileName);
                };

                dialog.ShowDialog();
            }
        }        
        
        private void mnuNewConfiguration_Click(object sender, EventArgs e)
        {
            LaunchNewTestEditor(false);
        }

        private void mnuNewConfigurationWithTemplate_Click(object sender, EventArgs e)
        {
            LaunchNewTestEditor(true);
        }

        private void mnuOpenConfigurationFile_Click(object sender, EventArgs e)
        {
            OpenConfigurationFile();
        }

        private void MDI_DragDrop(object sender, DragEventArgs e)
        {
            var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (fileNames == null) return;

            foreach (var testSuite in fileNames.Select(GetConfigurationSuiteFromFile))
            {
                LaunchNewTestEditor((testSuite));
            }
        }

        private void MDI_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(@"Are you sure you want to exit?", @"Are you sure?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _closingFlag = true;
                Application.Exit();
            }
                
        }

        private void Mdi_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_closingFlag)
            {
                return;
            }

            if (MessageBox.Show(@"Are you sure you want to exit?", @"Are you sure?", MessageBoxButtons.YesNo,
               MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
            
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var about = new AboutDialog())
            {
                about.ShowDialog();
            }
        }

        private void generateFileChecksumsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var checksumGenerator = new ChecksumGenerator())
            {
                checksumGenerator.ShowDialog();
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("http://stephenhaunts.com/projects/post-deployment-smoke-tester/articles-about-the-smoke-tester/");            
            }
            catch
            {
                MessageBox.Show(@"There was a problem loading the help pages.", @"Failed to load help pages.",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }            
        }

        private void generateFileListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var generateFileList = new FileListGenerator())
            {
                generateFileList.ShowDialog();
            }
        }
    }
}
