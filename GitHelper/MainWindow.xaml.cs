using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GitHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void MainWindow1_Loaded(object sender, System.EventArgs e)
        {
            //if the saved list is not empty...
            if (Properties.Settings.Default["listDir"].ToString() != "")
            {
                //add saved variable entries to listDir and combobox
                List<string> listDir = new List<string>(Properties.Settings.Default["listDir"].ToString().Split(new char[] { ';' }));
                foreach (string item in listDir)
                {
                    if (item != "")
                    {
                        cbDirs.Items.Add(item);
                    }
                }
            }

            //if there isn't a saved selected diretory or saved list...
            if (Properties.Settings.Default["selectedDir"].ToString() == "")
            {
                //let the user change the directory on bootup
                Change_Dir();
            } 
            else
            {
                //load the last selected directory
                cbDirs.SelectedItem = Properties.Settings.Default["selectedDir"].ToString();
                btnPerfOp.IsEnabled = true;
            }

            //if there was a selected operation in the last session...
            if (Properties.Settings.Default["selectedOp"].ToString() != "")
            {
                //go through each operation and see if it matches with a radio button
                if (Properties.Settings.Default["selectedOp"].ToString() == "git pull")
                {
                    rbPull.IsChecked = true;
                }
                else if (Properties.Settings.Default["selectedOp"].ToString() == "git push")
                {
                    rbPush.IsChecked = true;
                }
                else
                {
                    rbPull.IsChecked = true;
                }
            }

            //if there was a selected operation in the last session...
            if (Properties.Settings.Default["selectedSc"].ToString() != "")
            {
                //assign the appropriate scope
                if (Convert.ToBoolean(Properties.Settings.Default["selectedSc"]) == false)
                {
                    rbThisRepo.IsChecked = true;
                }
                else
                {
                    rbAllRepos.IsChecked = true;
                }
            }

            //create vars for branch check
            string directory = Properties.Settings.Default["selectedDir"].ToString();

            //use powershell to find and store branches of git repo
            List<string> branches = Git_Results(directory, "branch");

            //find current branch and set is as the selected branch. then add the rest!
            ReEval_Branches(branches, "*");
        }

        ///<summary>
        ///Changes tbBranches to display the currently selected branch from a list of branches.
        ///</summary>
        private void ReEval_Branches(List<string> branches, string selectsymbol)
        {
            //clear items
            tbBranches.Clear();

            //go through each branch and find the current one. add all of them to the combobox
            foreach (string branch in branches)
            {
                if (branch.Contains(selectsymbol))
                {
                    tbBranches.Text = branch.ToString();
                }
            }
        }

        ///<summary>
        ///Generates a string list of PowerShell output after executing a git command.
        ///</summary>
        private List<string> Git_Results(string directory, string gitcommand)
        {
            //set caption for the process
            lblFeedback.Visibility = Visibility.Visible;
            lblFeedback.Content = $"Working on git {gitcommand} on repo {directory}...";

            List<string> returnlist = new List<string>();

            //set up powershell
            PowerShell powershell = PowerShell.Create();
            powershell.AddScript($"cd {directory}");
            powershell.AddScript($"git {gitcommand}");

            //run it
            Collection<PSObject> results = powershell.Invoke();

            //iterate through the stdout
            foreach (PSObject result in results)
            {
                returnlist.Add(result.ToString());
            }

            //iterate through the stderr for git results
            foreach (ErrorRecord error in powershell.Streams.Error)
            {
                returnlist.Add(error.ToString());
            }

            lblFeedback.Visibility = Visibility.Hidden;
            return returnlist;
        }

        private void MainWindow1_Closed(object sender, System.EventArgs e)
        {
            //if there are items in the directory combobox...
            if (cbDirs.Items.Count > 0)
            {
                //create empty list
                string savedList = "";

                //write the combobox items into the string
                foreach (string item in cbDirs.Items)
                {
                    savedList += item + ";";
                }

                //remove the last semicolon
                savedList.Remove(savedList.Length - 1, 1);

                //save to app.config
                Properties.Settings.Default["listDir"] = savedList;
                Properties.Settings.Default.Save();
            }
        }


        private void General_Click(object sender, RoutedEventArgs e)
        {
            //create vars
            string directory = Properties.Settings.Default["selectedDir"].ToString();
            string currentbranch = "";

            //use powershell to check branches of git repo
            List<string> branches = Git_Results(directory, "branch");

            //save current branch as...currentbrach!
            foreach (string branch in branches)
            {
                if (branch.Contains("*"))
                {
                    currentbranch = branch.ToString();
                }
            }

            //update the user with the results
            System.Windows.MessageBox.Show($"Repo Directory: {directory}\n\n" +
                                           $"Current Branch: {currentbranch}\n\n" +
                                           $"Branches: " + string.Join(",", branches),"General Branch Info");
        }

        private void Remote_Click(object sender, RoutedEventArgs e)
        {
            string directory = Properties.Settings.Default["selectedDir"].ToString();
            string gitop = "remote -v";

            List<string> results = Git_Results(directory, gitop);

            System.Windows.MessageBox.Show($"Repo Directory: {directory}\n\n" +
                                           $"Remotes: \n {string.Join("\n",results)}","Remote Info");
        }

        private void AddBranch_Click(object sender, RoutedEventArgs e)
        {
            //ask the user for the desired name
            string inputresult = Interaction.InputBox("Enter the desired branch name: ", "Add Branch", "New Branch");

            //create a new branch and checkout to it
            string directory = Properties.Settings.Default["selectedDir"].ToString();
            List<string> results = Git_Results(directory, $"checkout -b {inputresult}");

            //update the user with the results
            List<string> branches = Git_Results(directory, "branch");
            ReEval_Branches(branches, "*");
            System.Windows.MessageBox.Show(string.Join("\n", results), "Add Branch");
        }

        private void DeleteBranch_Click(object sender, RoutedEventArgs e)
        {
            //retrieve the branch names so that the inputbox can show the user
            string directory = Properties.Settings.Default["selectedDir"].ToString();
            List<string> branches = Git_Results(directory, "branch");
            string inputresult = Interaction.InputBox($"Enter the branch name from the list below: {string.Join("\n",branches)}", "Delete Branch", "Branch Name");

            //check for common errors
            List<string> results = Git_Results(directory, $"branch -d {inputresult}");
            if (results.Contains($"error: The branch '{inputresult}' is not fully merged."))
            {
                if (System.Windows.MessageBox.Show($"The branch {inputresult} is not fully merged. Are you sure you want to delete?",
                                                    "Delete Branch", MessageBoxButton.YesNo).Equals(MessageBoxResult.Yes))
                {
                    results = Git_Results(directory, $"branch -D {inputresult}");
                }
            }

            //update the user with the results
            branches = Git_Results(directory, "branch");
            ReEval_Branches(branches, "*");
            System.Windows.MessageBox.Show(string.Join("\n", results), "Delete Branch");
        }

        private void BranchChange_Click(object sender, RoutedEventArgs e)
        {
            //create a list of branches from the selected repo
            string directory = Properties.Settings.Default["selectedDir"].ToString();
            List<string> branches = Git_Results(directory, "branch");

            //ask the user for their preferred branch
            string inputresult = Interaction.InputBox($"Enter the branch name from the list below:\n {string.Join("\n", branches)}", "Change Branch", "Branch Name");

            //if the user didn't close the window and the inputresult branch is valid...
            if (inputresult != "")
            {
                //reevaluate the branches to ensure that the inputresult branch is displayed in the textbox
                List<string> results = Git_Results(directory, $"checkout {inputresult}");
                branches = Git_Results(directory, "branch");
                ReEval_Branches(branches, "*");
                System.Windows.MessageBox.Show(string.Join("\n", results), "Change Branch");
            }
        }

        

        private void Change_Dir()
        {
            //disable the update button
            btnPerfOp.IsEnabled = false;

            //create folder dialog
            FolderBrowserDialog selectDir = new FolderBrowserDialog();
            selectDir.Description = "Select the repo folder:";

            //if they clicked ok...
            if (selectDir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //if a .git folder exists in their selection...
                if (Directory.Exists(System.IO.Path.Combine(selectDir.SelectedPath, ".git")))
                {
                    //if the combobox doesn't already have an item for the folder...
                    if (cbDirs.Items.Contains(selectDir.SelectedPath) == false)
                    {
                        //add a new folder item
                        cbDirs.Items.Add(selectDir.SelectedPath);
                    }

                    //change index to the selected folder item
                    cbDirs.SelectedIndex = cbDirs.Items.IndexOf(selectDir.SelectedPath);
                }

                //if a .git folder does not exist...
                else
                {
                    //if the user acknowledges the following error...
                    if (System.Windows.Forms.MessageBox.Show("This folder does not contain a git repository. Select a different folder.", "Error: no repository found", MessageBoxButtons.OK, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.OK)
                    {
                        //repeat this method
                        Change_Dir();
                    }
                }
            }

            //enable the update button
            btnPerfOp.IsEnabled = true;
        }

        private void AddLocalRepo_Click(object sender, RoutedEventArgs e)
        {
            Change_Dir();
        }

        private void AddRemoteRepo_Click(object sender, RoutedEventArgs e)
        {
            string inputresult = Interaction.InputBox("Please enter the URL for the remote repo:", "Add Remote Repo","Repo URL");

            if (inputresult.Length > 0)
            {
                FolderBrowserDialog folder = new FolderBrowserDialog();
                folder.Description = "Select the destination folder:";

                if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                { 
                    string directory = folder.SelectedPath;
                    List<string> results = Git_Results(directory, $"clone {inputresult}");
                    System.Windows.MessageBox.Show(string.Join("\n", results), "Add Remote Repo");
                    cbDirs.Items.Add(directory);
                }
            }
        }

        private void btnPerfOp_Click(object sender, RoutedEventArgs e)
        {
            //disable button in case user tries to click again during process
            btnPerfOp.IsEnabled = false;

            //create vars
            List<string> directories = new List<string>();
            List<string> output = new List<string>();
            bool scopeallrepos = Convert.ToBoolean(Properties.Settings.Default["selectedSc"]);
            string gitop = Properties.Settings.Default["selectedOp"].ToString();

            //if the selected scope is all saved repos...
            if (scopeallrepos)
            {
                //add the items from the combobox to created list
                foreach (string item in cbDirs.Items)
                {
                    directories.Add(item);
                }
            }
            //if the selected scope is this repo...
            else
            {
                //create list of one item. choosing to type this as a list instead of a string
                //has some advantages, including being able to iterate in the same way as a
                //more full list. that way, we don't have to write different code when it's
                //time to process the object.
                directories.Add(Properties.Settings.Default["selectedDir"].ToString());
            }

            //execute the git operation in powershell for each entry in the saved directory var.
            foreach (string directory in directories)
            {
                System.Windows.MessageBox.Show(string.Join("\n",Git_Results(directory, gitop)), $"Result of {gitop} for {directory}");
            }

            //enable the update button
            btnPerfOp.IsEnabled = true;
        }

        //whenever a button is pressed, change the saved config vars.
        private void cbDirs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Directory.Exists(cbDirs.SelectedItem.ToString()))
            {
                Properties.Settings.Default["selectedDir"] = cbDirs.SelectedItem;
                Properties.Settings.Default.Save();

                ReEval_Branches(Git_Results(cbDirs.SelectedItem.ToString(), "branch"), "*");
            }
            else
            {
                System.Windows.MessageBox.Show("The repository you selected has been deleted or moved. It will be removed from memory.", "Repository Change", MessageBoxButton.OK);
                cbDirs.Items.Remove(cbDirs.SelectedItem);
                cbDirs.SelectedIndex = 0;
            }
        }

        private void rbPush_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["selectedOp"] = "push";
            Properties.Settings.Default.Save();
        }

        private void rbPull_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["selectedOp"] = "pull";
            Properties.Settings.Default.Save();
        }

        private void rbThisRepo_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["selectedSc"] = false;
            Properties.Settings.Default.Save();
        }

        private void rbAllRepos_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["selectedSc"] = true;
            Properties.Settings.Default.Save();
        }
    }
}
