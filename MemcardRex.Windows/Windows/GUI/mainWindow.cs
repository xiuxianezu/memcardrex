//Main window of MemcardRex
//Shendo 2009-2024

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using MemcardRex.Core;

namespace MemcardRex
{
    [SupportedOSPlatform("windows")]
    public partial class mainWindow : Form
    {
        //List of open cards
        List<ps1card> PScard = new List<ps1card>();
        List<CardListView> cardList = new List<CardListView>();
        List<CardListView> historyList = new List<CardListView>();
        List<ImageList> iconList = new List<ImageList>();
        List<ImageList> historyIconList = new List<ImageList>();

        //Scaling values
        public double xScale = 1.0;
        public double yScale = 1.0;

        //Memory card data
        ps1card memCard = new ps1card();

        //Program settings
        public ProgramSettings appSettings = new ProgramSettings();

        //Plugin system
        public rexPluginSystem pluginSystem = new rexPluginSystem();
        int[] supportedPlugins = null;
        int[] clickedPlugin = new int[2];

        //Registerd hardware interfaces
        public List<HardInterfaces> registeredInterfaces = new List<HardInterfaces>();

        //Temp buffer for the save copy/paste
        byte[] tempBuffer = null;
        string tempBufferName = null;

        //Active card path and name
        string appPath = null;
        string appName = "MemcardRex 2.0 beta";
        string appVersion = null;
        string appDate = null;

        //Active slot to restore (for plugin operations)
        int pluginSlot = 0;

        public mainWindow()
        {
            InitializeComponent();
            Localization.ApplyToForm(this);
        }

        private void mainWindow_Shown(object sender, EventArgs e)
        {
            //Create new menu for managing plugins
            pluginSystem.fetchPlugins(appPath + "/Plugins");

            //Set the name of the application
            appName = "MemcardRex 2.0 beta";

            //Get version and date
            appVersion = typeof(mainWindow).Assembly.GetName().Version.ToString();
            appDate = File.GetLastWriteTime(typeof(mainWindow).Assembly.Location).ToString("yyyy-MM-dd");
        }

        //Create a new tab page
        private void createTabPage()
        {
            mainTabControl.TabPages.Add(PScard[PScard.Count - 1].cardName);
            makeListView();
        }

        //Open a Memory Card
        private void openCard(string fileName)
        {
            //Check if user wants to open a new card or a existing one
            if (fileName == null)
            {
                //Create a new card
                PScard.Add(new ps1card());
                PScard[PScard.Count - 1].cardName = Localization.T("New Memory Card");
                PScard[PScard.Count - 1].cardLocation = null;
            }
            else
            {
                //Open card file
                if (File.Exists(fileName) == true)
                {
                    //Open Memory Card file
                    PScard.Add(new ps1card());
                    PScard[PScard.Count - 1].OpenMemoryCard(fileName);
                }
                else
                {
                    MessageBox.Show(Localization.T("File was not found."), appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            //Create a tab page for the new card
            createTabPage();
        }

        //Check if the given ListView control is valid
        private bool validityCheck(out int listIndex, out int slotNumber)
        {
            listIndex = -1;
            slotNumber = -1;

            //Check if there are any cards open
            if (PScard.Count < 1) return false;
            if (cardList.Count < 1) return false;

            //Get current index
            listIndex = mainTabControl.SelectedIndex;

            //Check if tab page and the listview exist
            if (listIndex < 0 || cardList.Count < listIndex + 1) return false;

            //Check if there is an item selected
            if (cardList[listIndex].SelectedIndices.Count < 1) return false;
            slotNumber = cardList[listIndex].SelectedIndices[0];

            return true;
        }

        //Save a card to a file
        private void saveCardFunction(int listIndex)
        {
            if (!validityCheck(out listIndex, out int slotNumber)) return;

            if (PScard[listIndex].cardLocation == null)
            {
                saveCardDialog(listIndex);
                return;
            }

            //Save current Memory Card
            PScard[listIndex].SaveMemoryCard(PScard[listIndex].cardLocation);
        }

        //Save a Memory Card as dialog
        private void saveCardDialog(int listIndex)
        {
            if (!validityCheck(out listIndex, out int slotNumber)) return;

            SaveFileDialog saveCardDlg = new SaveFileDialog
            {
                Title = Localization.T("Save Memory Card"),
                Filter = "MemcardRex Memory Card|*.mc|PlayStation Memory Card|*.mcr|DexDrive Memory Card|*.gme",
                FilterIndex = appSettings.LastSaveFormat
            };

            if (saveCardDlg.ShowDialog() == DialogResult.OK)
            {
                appSettings.LastSaveFormat = saveCardDlg.FilterIndex;

                switch (saveCardDlg.FilterIndex)
                {
                    default:         //MemcardRex format
                        PScard[listIndex].SaveMemoryCard(saveCardDlg.FileName);
                        break;

                    case 2:         //PS1 standard format
                        PScard[listIndex].SavePS1MemoryCard(saveCardDlg.FileName);
                        break;

                    case 3:         //DexDrive format
                        PScard[listIndex].SaveDexDriveMemoryCard(saveCardDlg.FileName);
                        break;
                }

                refreshListView(listIndex, slotNumber);
                pushHistory(Localization.T("Card saved"), listIndex, prepareIcons(listIndex, slotNumber, false));
            }
        }

        //Open a Memory Card dialog
        private void openCardDialog()
        {
            OpenFileDialog openCardDlg = new OpenFileDialog
            {
                Title = Localization.T("Open Memory Card"),
                Filter = "MemcardRex Memory Card|*.mc|PlayStation Memory Card|*.mcr|DexDrive Memory Card|*.gme|All files (*.*)|*.*"
            };

            //If user selected a card open it
            if (openCardDlg.ShowDialog() == DialogResult.OK) openCard(openCardDlg.FileName);
        }

        //Save a card and its children
        private int closeAllCards()
        {
            int cardClosed = 0;

            //Check if there are any cards
            if (PScard.Count < 1) return cardClosed;

            //Check if any cards have been modified
            for (int i = 0; i < PScard.Count; i++)
            {
                if (PScard[i].changedFlag)
                {
                    //Ask user if he wants to save the card
                    if (MessageBox.Show(Localization.T("Save changes to ") + PScard[i].cardName + Localization.T("?"), appName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        //Just close the card
                        PScard[i].changedFlag = false;
                    }
                    else if (MessageBox.Show(Localization.T("Save changes to ") + PScard[i].cardName + Localization.T("?"), appName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                    {
                        //Do not close the card
                        cardClosed = 1;
                    }
                }
            }

            if (cardClosed == 0)
            {
                for (int i = 0; i < PScard.Count; i++)
                {
                    if (PScard[i].cardLocation != null)
                    {
                        //Save Memory Card
                        PScard[i].SaveMemoryCard(PScard[i].cardLocation);
                    }

                    PScard[i].Dispose();
                }

                PScard.Clear();
                cardList.Clear();
                historyList.Clear();
                iconList.Clear();
                historyIconList.Clear();

                mainTabControl.TabPages.Clear();
            }

            return cardClosed;
        }

        //Close the active card
        private void closeCard(int listIndex, bool dispose = true)
        {
            //Check if card should be saved
            if (PScard[listIndex].changedFlag)
            {
                if (MessageBox.Show(Localization.T("Save changes to ") + PScard[listIndex].cardName + Localization.T("?"), appName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    //Just close the card
                    PScard[listIndex].changedFlag = false;
                }
                else if (MessageBox.Show(Localization.T("Save changes to ") + PScard[listIndex].cardName + Localization.T("?"), appName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                {
                    //Do not close the card
                    return;
                }
            }

            //Check if card should be saved to a file
            if (PScard[listIndex].cardLocation != null)
            {
                PScard[listIndex].SaveMemoryCard(PScard[listIndex].cardLocation);
            }

            //Remove all traces of the card
            if (dispose) PScard[listIndex].Dispose();

            PScard.RemoveAt(listIndex);
            cardList.RemoveAt(listIndex);
            historyList.RemoveAt(listIndex);
            iconList.RemoveAt(listIndex);
            historyIconList.RemoveAt(listIndex);

            mainTabControl.TabPages.RemoveAt(listIndex);
        }

        //Close all cards
        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeAllCards();
        }

        //Close a card
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeCard(mainTabControl.SelectedIndex);
        }

        //Edit save comments
        private void editSaveComments()
        {
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            //Open comment editor
            commentsWindow commentsDlg = new commentsWindow();

            commentsDlg.initializeDialog(memCard.saveName[slotNumber] + Localization.T(" - Comments"), memCard.saveComments[slotNumber]);
            commentsDlg.ShowDialog(this);

            //Update save comments if OK was pressed
            if (commentsDlg.okPressed)
            {
                memCard.SetComments(slotNumber, commentsDlg.saveComment);

                refreshListView(listIndex, slotNumber);
                pushHistory(Localization.T("Comments edited"), listIndex, prepareIcons(listIndex, slotNumber, false));
            }

            commentsDlg.Dispose();
        }

        //Show save information
        private void showInformation()
        {
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            int masterSlot = memCard.GetMasterLinkForSlot(slotNumber);

            informationWindow infoDlg = new informationWindow();

            //Show info about the selected save
            infoDlg.initializeDialog(memCard.saveName[masterSlot], memCard.saveProdCode[masterSlot], memCard.saveIdentifier[masterSlot],
                memCard.saveRegion[masterSlot], memCard.saveType[masterSlot], memCard.saveSize[masterSlot], memCard.iconFrames[masterSlot],
                memCard.iconColorData[masterSlot], memCard.mcIconData[masterSlot], memCard.apIconData[masterSlot], memCard.iconDelay[masterSlot],
                memCard.GetOccupiedSlots(masterSlot), appSettings.IconBackgroundColor);

            infoDlg.ShowDialog(this);
            infoDlg.Dispose();
        }

        //Show save header edit dialog
        private void editSaveHeader()
        {
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            headerWindow headerDlg = new headerWindow();

            //Load values to dialog
            headerDlg.initializeDialog(appName, memCard.saveName[masterSlot] + Localization.T(" - Save header"), memCard.saveProdCode[masterSlot],
                memCard.saveIdentifier[masterSlot], memCard.saveRegion[masterSlot]);

            headerDlg.ShowDialog(this);

            //Update values if OK was pressed
            if (headerDlg.okPressed)
            {
                //Insert data to save header of the selected card and slot
                memCard.SetHeaderData(masterSlot, headerDlg.prodCode, headerDlg.saveIdentifier, headerDlg.saveRegion);

                refreshListView(listIndex, slotNumber);
                pushHistory(Localization.T("Header edited"), listIndex, prepareIcons(listIndex, masterSlot, false));
            }

            headerDlg.Dispose();
        }

        //Import a save
        private void importSaveDialog()
        {
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            //Check if the slot to import the save on is free
            if (PScard[listIndex].slotType[slotNumber] == (byte) ps1card.SlotTypes.formatted)
            {
                OpenFileDialog openFileDlg = new OpenFileDialog
                {
                    Title = Localization.T("Import save"),
                    Filter = ssSupportedExtensions + "|" + ssExtensions + "|RAW single save|*"
                };

                //If user selected a save load it
                if (openFileDlg.ShowDialog() == DialogResult.OK)
                {
                    if (PScard[listIndex].OpenSingleSave(openFileDlg.FileName, slotNumber, out int requiredSlots))
                    {
                        refreshListView(listIndex, slotNumber);
                        pushHistory(Localization.T("Save imported"), listIndex, prepareIcons(listIndex, slotNumber, false));
                    }
                    else if (requiredSlots > 0)
                    {
                        MessageBox.Show(Localization.T("To complete this operation ") + requiredSlots.ToString() + Localization.T(" free slots are required."), appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(Localization.T("The file could not be opened."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show(Localization.T("The selected slot is not empty."), appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //Export a save
        private void exportSaveDialog(bool isRaw)
        {
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            int masterSlot = memCard.GetMasterLinkForSlot(slotNumber);

            byte singleSaveType;
            string outputFilename;

            if (isRaw)
            {
                //RAW file name on the system
                outputFilename = memCard.saveRegionRaw[masterSlot] + PScard[listIndex].saveProdCode[masterSlot] + PScard[listIndex].saveIdentifier[masterSlot];
            }
            else
            {
                //Set output filename to be compatible with PS3
                byte[] identifierASCII = Encoding.ASCII.GetBytes(PScard[listIndex].saveIdentifier[masterSlot]);
                outputFilename = memCard.saveRegionRaw[masterSlot] + PScard[listIndex].saveProdCode[masterSlot] +
                    BitConverter.ToString(identifierASCII).Replace("-", "");
            }

            //This will help us preserve full file title if illegal characters were found in save file name
            int illegalCharCount = 0;
            string completeFileName = outputFilename;

            //Filter illegal characters from the name
            foreach (char illegalChar in "\\/\":*?<>|".ToCharArray())
            {
                if (outputFilename.Contains(illegalChar.ToString())) illegalCharCount++;
                outputFilename = outputFilename.Replace(illegalChar.ToString(), "");
            }

            SaveFileDialog saveFileDlg = new SaveFileDialog
            {
                Title = Localization.T("Export save"),
                FileName = outputFilename,
                Filter = ssExtensions,
                FilterIndex = appSettings.LastExportFormat
            };

            if (isRaw) saveFileDlg.Filter = "RAW single save|B???????????*";

            //If user selected a card save to it
            if (saveFileDlg.ShowDialog() == DialogResult.OK)
            {
                if (!isRaw && saveFileDlg.FilterIndex != appSettings.LastExportFormat)
                {
                    appSettings.LastExportFormat = saveFileDlg.FilterIndex;
                    //saveProgramSettings();
                }

                //Get save type
                switch (saveFileDlg.FilterIndex)
                {
                    default:         //MCS single save
                        singleSaveType = (int) ps1card.SingleSaveTypes.mcs;
                        break;

                    case 2:         //PS3 signed save
                        singleSaveType = (int) ps1card.SingleSaveTypes.psv;
                        break;

                    case 3:        //Action Replay
                        singleSaveType = (int) ps1card.SingleSaveTypes.psx;
                        break;
                }

                //RAW save type
                if (isRaw)
                {
                    singleSaveType = (int) ps1card.SingleSaveTypes.raw;

                    //Create text file with full file name if illegal characters were found
                    if (illegalCharCount > 0)
                    {
                        StreamWriter sw = File.CreateText(saveFileDlg.FileName + "_info.txt");
                        sw.WriteLine(completeFileName);
                        sw.WriteLine("");
                        sw.WriteLine(Localization.T("Region: \"") + memCard.saveRegion[masterSlot] + "\"");
                        sw.WriteLine(Localization.T("Product code: \"") + PScard[listIndex].saveProdCode[masterSlot] + "\"");
                        sw.WriteLine(Localization.T("Identifier: \"") + PScard[listIndex].saveIdentifier[masterSlot] + "\"");
                        sw.WriteLine("");
                        sw.WriteLine(Localization.T("This text file was created because the exported RAW save file name contains forbidden characters."));
                        sw.WriteLine(Localization.T("You can use this info when importing for example with uLaunchELF to make your save valid."));
                        sw.Write(Localization.T("Rename \"") + outputFilename + Localization.T("\" to \"") + completeFileName + Localization.T("\" after importing the save."));
                        sw.Close();
                    }
                }

                PScard[listIndex].SaveSingleSave(saveFileDlg.FileName, masterSlot, singleSaveType);
            }
        }

        //Import a save
        private void importSaveDialog()
        {
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            //Check if the slot to import the save on is free
            if (PScard[listIndex].slotType[slotNumber] == (byte) ps1card.SlotTypes.formatted)
            {
                OpenFileDialog openFileDlg = new OpenFileDialog
                {
                    Title = Localization.T("Import save"),
                    Filter = ssSupportedExtensions + "|" + ssExtensions + "|RAW single save|*"
                };

                //If user selected a save load it
                if (openFileDlg.ShowDialog() == DialogResult.OK)
                {
                    if (PScard[listIndex].OpenSingleSave(openFileDlg.FileName, slotNumber, out int requiredSlots))
                    {
                        refreshListView(listIndex, slotNumber);
                        pushHistory(Localization.T("Save imported"), listIndex, prepareIcons(listIndex, slotNumber, false));
                    }
                    else if (requiredSlots > 0)
                    {
                        MessageBox.Show(Localization.T("To complete this operation ") + requiredSlots.ToString() + Localization.T(" free slots are required."), appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(Localization.T("The file could not be opened."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show(Localization.T("The selected slot is not empty."), appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //Export a save
        private void exportSaveDialog(bool isRaw)
        {
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            int masterSlot = memCard.GetMasterLinkForSlot(slotNumber);

            byte singleSaveType;
            string outputFilename;

            if (isRaw)
            {
                //RAW file name on the system
                outputFilename = memCard.saveRegionRaw[masterSlot] + PScard[listIndex].saveProdCode[masterSlot] + PScard[listIndex].saveIdentifier[masterSlot];
            }
            else
            {
                //Set output filename to be compatible with PS3
                byte[] identifierASCII = Encoding.ASCII.GetBytes(PScard[listIndex].saveIdentifier[masterSlot]);
                outputFilename = memCard.saveRegionRaw[masterSlot] + PScard[listIndex].saveProdCode[masterSlot] +
                    BitConverter.ToString(identifierASCII).Replace("-", "");
            }

            //This will help us preserve full file title if illegal characters were found in save file name
            int illegalCharCount = 0;
            string completeFileName = outputFilename;

            //Filter illegal characters from the name
            foreach (char illegalChar in "\\/\":*?<>|".ToCharArray())
            {
                if (outputFilename.Contains(illegalChar.ToString())) illegalCharCount++;
                outputFilename = outputFilename.Replace(illegalChar.ToString(), "");
            }

            SaveFileDialog saveFileDlg = new SaveFileDialog
            {
                Title = Localization.T("Export save"),
                FileName = outputFilename,
                Filter = ssExtensions,
                FilterIndex = appSettings.LastExportFormat
            };

            if (isRaw) saveFileDlg.Filter = "RAW single save|B???????????*";

            //If user selected a card save to it
            if (saveFileDlg.ShowDialog() == DialogResult.OK)
            {
                if (!isRaw && saveFileDlg.FilterIndex != appSettings.LastExportFormat)
                {
                    appSettings.LastExportFormat = saveFileDlg.FilterIndex;
                    //saveProgramSettings();
                }

                //Get save type
                switch (saveFileDlg.FilterIndex)
                {
                    default:         //MCS single save
                        singleSaveType = (int) ps1card.SingleSaveTypes.mcs;
                        break;

                    case 2:         //PS3 signed save
                        singleSaveType = (int) ps1card.SingleSaveTypes.psv;
                        break;

                    case 3:        //Action Replay
                        singleSaveType = (int) ps1card.SingleSaveTypes.psx;
                        break;
                }

                //RAW save type
                if (isRaw)
                {
                    singleSaveType = (int) ps1card.SingleSaveTypes.raw;

                    //Create text file with full file name if illegal characters were found
                    if (illegalCharCount > 0)
                    {
                        StreamWriter sw = File.CreateText(saveFileDlg.FileName + "_info.txt");
                        sw.WriteLine(completeFileName);
                        sw.WriteLine("");
                        sw.WriteLine(Localization.T("Region: \"") + memCard.saveRegion[masterSlot] + "\"");
                        sw.WriteLine(Localization.T("Product code: \"") + PScard[listIndex].saveProdCode[masterSlot] + "\"");
                        sw.WriteLine(Localization.T("Identifier: \"") + PScard[listIndex].saveIdentifier[masterSlot] + "\"");
                        sw.WriteLine("");
                        sw.WriteLine(Localization.T("This text file was created because the exported RAW save file name contains forbidden characters."));
                        sw.WriteLine(Localization.T("You can use this info when importing for example with uLaunchELF to make your save valid."));
                        sw.Write(Localization.T("Rename \"") + outputFilename + Localization.T("\" to \"") + completeFileName + Localization.T("\" after importing the save."));
                        sw.Close();
                    }
                }

                PScard[listIndex].SaveSingleSave(saveFileDlg.FileName, masterSlot, singleSaveType);
            }
        }

        //Open preferences window
        private void editPreferences()
        {
            new preferencesWindow().initializeDialog(this, registeredInterfaces);
            EnableDisableHardwareMenus();

            //Refresh current list after preferences change
            if(!validityCheck(out int listIndex, out int slotNumber)) return;
            refreshListView(listIndex, slotNumber);
        }

        //Open edit icon dialog
        private void editIcon()
        {
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            int masterSlot = memCard.GetMasterLinkForSlot(slotNumber);
            iconWindow iconDlg = new iconWindow();

            iconDlg.gridColorValue = appSettings.GridColorValue;
            iconDlg.gridEnabled = appSettings.IconGridEnabled == 1;
            iconDlg.initializeDialog(memCard.saveName[masterSlot], memCard.iconFrames[masterSlot], memCard.GetIconBytes(masterSlot));
            iconDlg.ShowDialog(this);

            appSettings.GridColorValue = iconDlg.gridColorValue;
            appSettings.IconGridEnabled = iconDlg.gridEnabled ? 1 : 0;

            //Update data if OK has been pressed
            if (iconDlg.okPressed)
            {
                PScard[listIndex].SetIconBytes(masterSlot, iconDlg.iconData);
                refreshListView(listIndex, slotNumber);
                pushHistory(Localization.T("Icon edited"), listIndex, prepareIcons(listIndex, masterSlot, false));
            }

            iconDlg.Dispose();
        }

        //Create and show plugins dialog
        private void showPluginsWindow()
        {
            pluginsWindow pluginsDlg = new pluginsWindow();

            pluginsDlg.initializeDialog(this, pluginSystem.assembliesMetadata);
            pluginsDlg.ShowDialog(this);
            pluginsDlg.Dispose();
        }

        //Create and show about dialog
        private void showAbout()
        {
            new AboutWindow().initDialog(this, appName, appVersion, appDate, Localization.T("Copyright © Shendo 2025"),
                Localization.T("Authors: Alvaro Tanarro, bitrot-alpha, lmiori92, \nNico de Poel, KuromeSan, Robxnano, Shendo.\n\n") +
                Localization.T("Beta testers: Gamesoul Master, Xtreme2damax,\nCarmax91, NKO. \n\n") +
                Localization.T("Thanks to: @ruantec, Cobalt, TheCloudOfSmoke,\nRedawgTS, Hard core Rikki, RainMotorsports,\nZieg, Bobbi, OuTman, Kevstah2004, Kubusleonidas, \nFrédéric Brière, Cor'e, Gemini, DeadlySystem, \nPadraig Flood, Martin Korth (nocash).\n\n") +
                Localization.T("Special thanks to the following people whose\nMemory Card utilities inspired me to write my own:\nSimon Mallion (PSXMemTool),\nLars Ole Dybdal (PSXGameEdit),\nAldo Vargas (Memory Card Manager),\nNeill Corlett (Dexter),\nPaul Phoneix (ConvertM)."));
        }

        //Bring a new item to the history list
        private void pushHistory(string description, int listIndex, Bitmap saveIcon)
        {

            //Clean everything from current index to end
            if (historyList[listIndex].SelectedIndices.Count > 0)
            {
                while (historyList[listIndex].SelectedIndices[0] < historyList[listIndex].Items.Count - 1)
                {
                    historyList[listIndex].Items.RemoveAt(historyList[listIndex].Items.Count - 1);
                    historyIconList[listIndex].Images.RemoveAt(historyIconList[listIndex].Images.Count - 1);
                }
            }

            //Add save icon to history list
            historyIconList[listIndex].Images.Add(saveIcon);

            //Add item to list
            historyList[listIndex].Items.Add(description);

            //Select the added item
            historyList[listIndex].Items[historyList[listIndex].Items.Count - 1].Selected = true;

            //Make sure that it's visible
            historyList[listIndex].Items[historyList[listIndex].Items.Count - 1].EnsureVisible();
        }

        //Make a new ListView control
        private void makeListView()
        {
            int listIndex = mainTabControl.TabPages.Count - 1;
            int tabWidth = mainTabControl.TabPages[listIndex].Width;

            //Add a new ImageList to hold the card icons
            iconList.Add(new ImageList());
            iconList[listIndex].ImageSize = new Size((int)(xScale * 48), (int)(yScale * 16));
            iconList[listIndex].ColorDepth = ColorDepth.Depth32Bit;

            //Also for history list
            historyIconList.Add(new ImageList());
            historyIconList[listIndex].ImageSize = new Size((int)(xScale * 16), (int)(yScale * 16));
            historyIconList[listIndex].ColorDepth = ColorDepth.Depth32Bit;

            //Add history list
            historyList.Add(new CardListView());
            historyList[listIndex].Font = new Font(FontFamily.GenericSansSerif.Name, 8.25f);
            historyList[listIndex].Location = new Point(512, 0);
            historyList[listIndex].Dock = DockStyle.Right;
            historyList[listIndex].Size = new Size((int)(xScale * 160), 300);
            historyList[listIndex].BorderStyle = BorderStyle.None;
            historyList[listIndex].BackColor = ActiveColors.backColor;
            historyList[listIndex].ForeColor = ActiveColors.foreColor;
            historyList[listIndex].HeaderStyle = ColumnHeaderStyle.Nonclickable;
            historyList[listIndex].FullRowSelect = true;
            historyList[listIndex].MultiSelect = false;
            historyList[listIndex].HideSelection = false;
            historyList[listIndex].Columns.Add(Localization.T("History"));
            historyList[listIndex].Columns[0].Width = (int)(xScale * 160);
            historyList[listIndex].View = View.Details;
            historyList[listIndex].SelectedIndexChanged += new System.EventHandler(this.historyList_IndexChanged);
            historyList[listIndex].SmallImageList = historyIconList[historyIconList.Count - 1];

            cardList.Add(new CardListView());
            cardList[listIndex].Font = new Font(FontFamily.GenericSansSerif.Name, 8.25f);
            cardList[listIndex].BorderStyle = BorderStyle.None;
            cardList[listIndex].Size = new Size(tabWidth - (int)(xScale * 160), 300);
            cardList[listIndex].BackColor = ActiveColors.backColor;
            cardList[listIndex].ForeColor = ActiveColors.foreColor;
            cardList[listIndex].Dock = DockStyle.Bottom | DockStyle.Top | DockStyle.Left;
            cardList[listIndex].SmallImageList = iconList[iconList.Count - 1];
            cardList[listIndex].ContextMenuStrip = mainContextMenu;
            cardList[listIndex].FullRowSelect = true;
            cardList[listIndex].MultiSelect = false;
            cardList[listIndex].HeaderStyle = ColumnHeaderStyle.Nonclickable;
            cardList[listIndex].HideSelection = false;
            cardList[listIndex].Columns.Add(Localization.T("Icon, region and title"));
            cardList[listIndex].Columns.Add(Localization.T("Product code"));
            cardList[listIndex].Columns.Add(Localization.T("Identifier"));
            cardList[listIndex].Columns[0].Width = (int)(xScale * 312);
            cardList[listIndex].Columns[1].Width = (int)(xScale * 98);
            cardList[listIndex].Columns[2].Width = tabWidth - (int)(xScale * 312) - (int)(xScale * 98) - (int)(xScale * 160);
            cardList[listIndex].View = View.Details;
            cardList[listIndex].DoubleClick += new System.EventHandler(this.cardList_DoubleClick);
            cardList[listIndex].SelectedIndexChanged += new System.EventHandler(this.cardList_IndexChanged);

            refreshListView(listIndex, -1);
        }
        
        //Refresh the ListView
        private void refreshListView(int listIndex, int slotNumber)
        {
            //Place cardName on the tab
            if(mainTabControl.TabPages[listIndex].Text != PScard[listIndex].cardName)
                mainTabControl.TabPages[listIndex].Text = PScard[listIndex].cardName;

            //Remove all icons from the list
            iconList[listIndex].Images.Clear();

            //Remove all items from the list
            cardList[listIndex].Items.Clear();

            for (int i = 0; i < ps1card.SlotCount; i++)
            {
                switch (PScard[listIndex].slotType[i])
                {
                    default:
                        iconList[listIndex].Images.Add(prepareIcons(listIndex, i, true));
                        cardList[listIndex].Items.Add(PScard[listIndex].saveName[i]);

                        //Check if save is using non standard region and append it to product code if not
                        //this fixes info for FreePSXBoot, soundscope, codelist and bunch of other nonstandard save names
                        if (PScard[listIndex].saveRegion[i] == "America" ||
                            PScard[listIndex].saveRegion[i] == "Europe" ||
                            PScard[listIndex].saveRegion[i] == "Japan")
                        {
                            cardList[listIndex].Items[i].SubItems.Add(PScard[listIndex].saveProdCode[i]);
                        }
                        else
                        {
                            cardList[listIndex].Items[i].SubItems.Add(PScard[listIndex].saveRegion[i] + PScard[listIndex].saveProdCode[i]);
                        }

                        cardList[listIndex].Items[i].SubItems.Add(PScard[listIndex].saveIdentifier[i]);
                        cardList[listIndex].Items[i].ImageIndex = i;
                        break;

                    case ps1card.SlotTypes.formatted:
                        cardList[listIndex].Items.Add(Localization.T("Free slot"));
                        iconList[listIndex].Images.Add(new Bitmap(48, 16));
                        break;

                    case ps1card.SlotTypes.corrupted:
                        cardList[listIndex].Items.Add(Localization.T("Corrupted slot"));
                        iconList[listIndex].Images.Add(new Bitmap(48, 16));
                        break;
                }
            }

            //Select the active item in the list
            if(slotNumber >= 0) cardList[listIndex].Items[slotNumber].Selected = true;

            //Set showListGrid option
            cardList[listIndex].GridLines = appSettings.ShowListGrid == 1;

            refreshPluginBindings();

            //Enable certain list items
            enableSelectiveEditItems();

            //Enable or disable undo/redo menu items
            enableDisableUndoRedo();
        }

        //Prepare icons for drawing (add flags and make them transparent if save is deleted)
        private Bitmap prepareIcons(int listIndex, int slotNumber, bool withFlag)
        {
            Bitmap iconBitmap;

            if (withFlag) iconBitmap = new Bitmap(48, 16);
            else iconBitmap = new Bitmap(16, 16);

            Graphics iconGraphics = Graphics.FromImage(iconBitmap);
            BmpBuilder bmpImage = new BmpBuilder();
            Bitmap saveIcon = new Bitmap(new MemoryStream(bmpImage.BuildBmp(PScard[listIndex].iconColorData[slotNumber, 0])));

            //Check what background color should be set
            switch (appSettings.IconBackgroundColor)
            {
                case 1:     //Black
                    iconGraphics.FillRegion(new SolidBrush(Color.Black), new Region(new Rectangle(0, 0, 16, 16)));
                    break;

                case 2:     //Gray
                    iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x30, 0x30, 0x30)), new Region(new Rectangle(0, 0, 16, 16)));
                    break;

                case 3:     //Blue
                    iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xFF, 0x44, 0x44, 0x98)), new Region(new Rectangle(0, 0, 16, 16)));
                    break;
            }

            //Draw icon
            iconGraphics.DrawImage(saveIcon, 0, 0, 16, 16);

            switch (PScard[listIndex].slotType[slotNumber])
            {
                case ps1card.SlotTypes.deleted_initial:
                case ps1card.SlotTypes.deleted_middle_link:
                case ps1card.SlotTypes.deleted_end_link:
                    Color blendColor = cardList[listIndex].BackColor;
                    iconGraphics.FillRegion(new SolidBrush(Color.FromArgb(0xA0, blendColor.R, blendColor.G, blendColor.B)), 
                        new Region(new Rectangle(0, 0, 16, 16)));
                    break;
            }

            //Skip drawing flag
            if (!withFlag)
            {
                iconGraphics.Dispose();
                return iconBitmap;
            }

            //Draw flag depending of the region
            switch (PScard[listIndex].saveRegion[slotNumber])
            {
                case "America":    //American region
                    iconGraphics.DrawImage(Properties.Resources.amflag, 17, 0, 30, 16);
                    break;

                case "Europe":    //European region
                    iconGraphics.DrawImage(Properties.Resources.euflag, 17, 0, 30, 16);
                    break;

                case "Japan":    //Japanese region
                    iconGraphics.DrawImage(Properties.Resources.jpflag, 17, 0, 30, 16);
                    break;
            }

            //Draw comment icon if save contains a comment
            if(PScard[listIndex].saveComments[slotNumber].Length > 0)
            {
                if(PScard[listIndex].saveComments[slotNumber][0] != '\0')
                iconGraphics.DrawImage(Properties.Resources.comments, 38, 6, 8, 8);
            }

            iconGraphics.Dispose();

            return iconBitmap;
        }

        //Refresh the toolstrip
        private void refreshStatusStrip()
        {
            //Show the location of the active card in the tool strip (if there are any cards)
            if (PScard.Count > 0)
                toolString.Text = PScard[mainTabControl.SelectedIndex].cardLocation + " ";
            else
                toolString.Text = " ";
        }

        //Save work and close the application
        private void exitApplication(FormClosingEventArgs e)
        {
            //Close every opened card
            if (closeAllCards() == 1)
                e.Cancel = true;

            //Write window position
            appSettings.WindowPositionX = this.Left;
            appSettings.WindowPositionY = this.Top;

            //Save settings
            appSettings.SaveSettings(appPath, appName, appVersion);
        }

        //Edit header of the selected save
        private void editSaveHeader()
        {
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            headerWindow headerDlg = new headerWindow();
            int masterSlot = memCard.GetMasterLinkForSlot(slotNumber);

            //Load values to dialog
            headerDlg.initializeDialog(appName, memCard.saveName[masterSlot], memCard.saveProdCode[masterSlot],
                memCard.saveIdentifier[masterSlot], memCard.saveRegion[masterSlot]);
            headerDlg.ShowDialog(this);

            //Update values if OK was pressed
            if (headerDlg.okPressed)
            {
                //Insert data to save header of the selected card and slot
                memCard.SetHeaderData(masterSlot, headerDlg.prodCode, headerDlg.saveIdentifier, headerDlg.saveRegion);
                refreshListView(listIndex, slotNumber);
                pushHistory(Localization.T("Header edited"), mainTabControl.SelectedIndex, prepareIcons(listIndex, masterSlot, false));
            }
            headerDlg.Dispose();
        }

        //Open a card if it's given by command line
        private bool loadCommandLine()
        {
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                openCard(Environment.GetCommandLineArgs()[1]);
                return true;
            }
            else return false;
        }

        private void mainWindowLoad(object sender, EventArgs e)
        {
            //Show name of the application on the mainWindow
            this.Text = appName + " " + appVersion;

            //Load settings from settings file
            appSettings.LoadSettings(appPath);

            //Restore saved window position if needed
            if(appSettings.RestoreWindowPosition == 1)
            {
                this.Left = appSettings.WindowPositionX;
                this.Top = appSettings.WindowPositionY;
            }

            //Set active interface after loaded settings
            EnableDisableHardwareMenus();

            //Load available plugins
            pluginSystem.fetchPlugins(appPath + "/Plugins");

            //Create an empty card upon startup or load one given by the command line
            if(loadCommandLine() == false)openCard(null);
        }

        private void managePluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Show the plugins dialog
            showPluginsWindow();
        }

        //Refresh plugin menu
        private void refreshPluginBindings()
        {
            //Clear the menus
            editWithPluginToolStripMenuItem.DropDownItems.Clear();
            editWithPluginToolStripMenuItem.Enabled = false;

            editWithPluginToolStripMenuItem1.DropDownItems.Clear();
            editWithPluginToolStripMenuItem1.Enabled = false;

            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            //Get the supported plugins
            supportedPlugins = pluginSystem.getSupportedPlugins(memCard.saveProdCode[memCard.GetMasterLinkForSlot(slotNumber)]);

            //Check if there are any plugins that support the product code
            if (supportedPlugins != null)
            {
                //Enable plugin menu
                editWithPluginToolStripMenuItem.Enabled = true;
                editWithPluginToolStripMenuItem1.Enabled = true;

                int count = 0;

                foreach (int currentAssembly in supportedPlugins)
                {
                    //Add item to the plugin menu
                    editWithPluginToolStripMenuItem.DropDownItems.Add(pluginSystem.assembliesMetadata[currentAssembly].pluginName);
                    editWithPluginToolStripMenuItem1.DropDownItems.Add(pluginSystem.assembliesMetadata[currentAssembly].pluginName);

                    count++;

                    //Color it with the parent decorations
                    editWithPluginToolStripMenuItem.DropDownItems[count - 1].ForeColor = editWithPluginToolStripMenuItem.ForeColor;
                    editWithPluginToolStripMenuItem1.DropDownItems[count - 1].ForeColor = editWithPluginToolStripMenuItem1.ForeColor;
                }
            }
        }

        //Edit a selected save with a selected plugin
        private void editWithPlugin(int pluginIndex)
        {
            //Check if there are any cards to edit
            if (PScard.Count > 0)
            {
                int listIndex = mainTabControl.SelectedIndex;
                int slotNumber = memCard.GetMasterLinkForSlot(cardList[listIndex].SelectedIndices[0]);
                byte[] editedSaveBytes = pluginSystem.editSaveData(supportedPlugins[pluginIndex], PScard[listIndex].GetSaveBytes(slotNumber), PScard[listIndex].saveProdCode[slotNumber]);

                if (editedSaveBytes != null)
                {
                    PScard[listIndex].ReplaceSaveBytes(slotNumber, editedSaveBytes);

                    //Refresh the list with new data
                    refreshListView(listIndex, slotNumber);

                    //Set the edited flag of the card
                    PScard[listIndex].changedFlag = true;

                    pushHistory(Localization.T("Edited by plugin"), mainTabControl.SelectedIndex, prepareIcons(listIndex, slotNumber, false));
                }
            }
        }

        private void readMeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Check if Readme.txt exists
            if (File.Exists(appPath + "/Readme.txt")) System.Diagnostics.Process.Start(appPath + "/Readme.txt");
            else MessageBox.Show(Localization.T("'ReadMe.txt' was not found."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        //Enable or disable undo and redo items
        private void enableDisableUndoRedo()
        {
            undoToolStripMenuItem.Enabled = (memCard.UndoCount > 0);
            redoToolStripMenuItem.Enabled = (memCard.RedoCount > 0);
        }

        //Enable only supported edit operations
        private void enableSelectiveEditItems()
        {
            //Start with everything disabled
            SetEditItemsState(false);

            //Do not enable items if no cards are open
            if (mainTabControl.TabCount < 1) return;

            //Current card list we are working on
            CardListView currentCardList = cardList[mainTabControl.SelectedIndex];

            //Do not enable items if nothing is selected
            if (currentCardList.SelectedIndices.Count < 1) return;

            //Enable menu items based on the content
            switch (memCard.slotType[memCard.masterSlot[currentCardList.SelectedIndices[0]]])
            {
                case ps1card.SlotTypes.formatted:
                    importSaveToolStripMenuItem.Enabled = true;
                    importSaveToolStripMenuItem1.Enabled = true;
                    importButton.Enabled = true;

                    if (tempBuffer != null)
                    {
                        pasteSaveFromTemporaryBufferToolStripMenuItem.Enabled = true;
                        paseToolStripMenuItem.Enabled = true;
                    }
                    break;

                case ps1card.SlotTypes.initial:
                    SetEditItemsState(true);

                    restoreSaveToolStripMenuItem.Enabled = false;
                    restoreSaveToolStripMenuItem1.Enabled = false;
                    importSaveToolStripMenuItem.Enabled = false;
                    importSaveToolStripMenuItem1.Enabled = false;
                    importButton.Enabled = false;
                    pasteSaveFromTemporaryBufferToolStripMenuItem.Enabled = false;
                    paseToolStripMenuItem.Enabled = false;
                    break;

                case ps1card.SlotTypes.deleted_initial:
                    SetEditItemsState(true);

                    deleteSaveToolStripMenuItem.Enabled = false;
                    deleteSaveToolStripMenuItem1.Enabled = false;
                    importSaveToolStripMenuItem.Enabled = false;
                    importSaveToolStripMenuItem1.Enabled = false;
                    importButton.Enabled = false;
                    pasteSaveFromTemporaryBufferToolStripMenuItem.Enabled = false;
                    paseToolStripMenuItem.Enabled = false;
                    break;

                case ps1card.SlotTypes.corrupted:
                    //Enable only formating of the slot
                    removeSaveformatSlotsToolStripMenuItem.Enabled = true;
                    removeSaveformatSlotsToolStripMenuItem1.Enabled = true;
                    break;
            }

        }

        //Compare currently selected save with the temp buffer
        private void compareSaveWithTemp()
        {
            //Save data to work with
            byte[] fetchedData = null;
            string fetchedDataTitle = null;

            //Check if temp buffer contains anything
            if (tempBuffer == null)
            {
               MessageBox.Show(Localization.T("Temp buffer is empty. Save can't be compared."), appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            //Get data to work with
            fetchedData = memCard.GetSaveBytes(memCard.GetMasterLinkForSlot(slotNumber));
            fetchedDataTitle = memCard.saveName[memCard.GetMasterLinkForSlot(slotNumber)];

            //Check if selected saves have the same size
            if (fetchedData.Length != tempBuffer.Length)
            {
                MessageBox.Show(Localization.T("Save file size mismatch. Saves can't be compared."), appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //Show compare window
            new compareWindow().initializeDialog(this, appName, fetchedData, fetchedDataTitle, tempBuffer, tempBufferName + Localization.T(" (temp buffer)"));
        }

        //Read a Memory Card from the physical device
        private void cardReaderRead(byte[] readData, string deviceName)
        {
            //Create a new card
            PScard.Add(new ps1card());

            //Fill the card with the new data
            PScard[PScard.Count - 1].OpenMemoryCardStream(readData, appSettings.FixCorruptedCards == 1);

            //Temporary set a bogus file location (to fool filterNullCard function)
            PScard[PScard.Count - 1].cardLocation = "\0";

            //Create a tab page for the new card
            createTabPage();

            //Restore null location since DexDrive Memory Card is not a file present on the Hard Disk
            PScard[PScard.Count - 1].cardLocation = null;

            //Set the info to history list
            pushHistory(Localization.T("Card read (") + deviceName + Localization.T(")"), historyList.Count - 1, new Bitmap(16, 16));
        }

        private void mainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Show the location of the active card in the tool strip
            refreshStatusStrip();

            //Load available plugins for the selected save
            refreshPluginBindings();

            //Enable certain list items
            enableSelectiveEditItems();

            //Enable undo/redo menu items
            enableDisableUndoRedo();

            //Refresh active listview
            if (!validityCheck(out int listIndex, out int slotNumber)) return;
            refreshListView(listIndex, slotNumber);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Show about dialog
            showAbout();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Show browse dialog and open a selected Memory Card
            openCardDialog();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Close the application
            this.Close();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Create a new card by giving a null path
            openCard(null);
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Close the selected card
            closeCard(mainTabControl.SelectedIndex);
        }

        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Close all opened cards
            closeAllCards();
        }

        private void editSaveHeaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Edit header of the selected save
            editSaveHeader();
        }

        private void editSaveHeaderToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Edit header of the selected save
            editSaveHeader();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Save a Memory Card as...
            saveCardDialog(mainTabControl.SelectedIndex);
        }

        private void newButton_Click(object sender, EventArgs e)
        {
            //Create a new card by giving a null path
            openCard(null);
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            //Show browse dialog and open a selected Memory Card
            openCardDialog();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            //Save a Memory Card
            saveCardFunction(mainTabControl.SelectedIndex);
        }

        private void editSaveCommentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Edit save comment of the selected slot
            editSaveComments();
        }

        private void editSaveCommentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Edit save comment of the selected slot
            editSaveComments();
        }

        private void commentsButton_Click(object sender, EventArgs e)
        {
            //Edit save comment of the selected slot
            editSaveComments();
        }

        private void cardList_DoubleClick(object sender, EventArgs e)
        {
            //Show information about the selected save
            showInformation();
        }

        //Apply back and fore colors to all items and subitems in the list
        private void colorListItem(ListView lv, int index, Color back, Color fore)
        {
            lv.Items[index].BackColor = back;
            lv.Items[index].ForeColor = fore;

            //And subitems
            if (lv.Items[index].SubItems.Count > 1)
            {
                lv.Items[index].SubItems[1].BackColor = back;
                lv.Items[index].SubItems[1].ForeColor = fore;

                lv.Items[index].SubItems[2].BackColor = back;
                lv.Items[index].SubItems[2].ForeColor = fore;
            }
        }

        //Enable or disable menu items based on the currently selected save
        private void SetEditItemsState(bool itemStates)
        {
            //Edit menu
            editSaveHeaderToolStripMenuItem.Enabled = itemStates;
            editSaveCommentToolStripMenuItem.Enabled = itemStates;
            compareWithTempBufferToolStripMenuItem.Enabled = itemStates;
            editIconToolStripMenuItem.Enabled = itemStates;
            deleteSaveToolStripMenuItem.Enabled = itemStates;
            restoreSaveToolStripMenuItem.Enabled = itemStates;
            removeSaveformatSlotsToolStripMenuItem.Enabled = itemStates;
            copySaveToTempraryBufferToolStripMenuItem.Enabled = itemStates;
            pasteSaveFromTemporaryBufferToolStripMenuItem.Enabled = itemStates;
            importSaveToolStripMenuItem.Enabled = itemStates;
            exportSaveToolStripMenuItem.Enabled = itemStates;
            exportRAWSaveToolStripMenuItem.Enabled = itemStates;

            //Edit toolbar
            editHeaderButton.Enabled = itemStates;
            commentsButton.Enabled = itemStates;
            editIconButton.Enabled = itemStates;
            importButton.Enabled = itemStates;
            exportButton.Enabled = itemStates;

            //Edit popup
            editSaveHeaderToolStripMenuItem1.Enabled = itemStates;
            editSaveCommentsToolStripMenuItem.Enabled = itemStates;
            compareWithTempBufferToolStripMenuItem1.Enabled = itemStates;
            editIconToolStripMenuItem1.Enabled = itemStates;
            deleteSaveToolStripMenuItem1.Enabled = itemStates;
            restoreSaveToolStripMenuItem1.Enabled = itemStates;
            removeSaveformatSlotsToolStripMenuItem1.Enabled = itemStates;
            copySaveToTempBufferToolStripMenuItem.Enabled = itemStates;
            paseToolStripMenuItem.Enabled = itemStates;
            importSaveToolStripMenuItem1.Enabled = itemStates;
            exportSaveToolStripMenuItem1.Enabled = itemStates;
            exportRAWSaveToolStripMenuItem1.Enabled = itemStates;
            saveInformationToolStripMenuItem.Enabled = itemStates;
        }

        private void historyList_IndexChanged(object sender, EventArgs e)
        {
            var selectedList = sender as ListView;

            //If nothing is selected abort
            if (selectedList.SelectedIndices.Count < 1) return;

            int selectedIndex = selectedList.SelectedIndices[0];

            //Set item colors
            for (int i = 0; i < selectedList.Items.Count; i++)
            {
                if (selectedList.Items[i].Selected)
                colorListItem(selectedList, i, Color.FromArgb(220, Color.FromKnownColor(KnownColor.MenuHighlight)), SystemColors.HighlightText);
                else
                colorListItem(selectedList, i, selectedList.BackColor, selectedList.ForeColor);
            }

            //Jump to the selected point in time
            if(memCard.UndoCount > selectedIndex)
            {
                while(memCard.UndoCount > selectedIndex) memCard.Undo();
            }
            else if (memCard.UndoCount < selectedIndex)
            {
                while (memCard.UndoCount < selectedIndex) memCard.Redo();
            }

            //Refresh list so the updates can be visible
            validityCheck(out int listIndex, out int slotNumber);
            refreshListView(listIndex, slotNumber);
        }

        private void cardList_IndexChanged(object sender, EventArgs e)
        {
            var selectedList = sender as ListView;

            SetEditItemsState(false);

            //If nothing is selected abort
            if (selectedList.SelectedIndices.Count < 1) return;

            //Reset colors to default
            for (int i = 0; i < ps1card.SlotCount; i++)
            {
                colorListItem(selectedList, i , selectedList.BackColor, selectedList.ForeColor);
            }

            //Select all linked save slots
            foreach(int slotIndex in memCard.FindSaveLinks(memCard.GetMasterLinkForSlot(selectedList.SelectedIndices[0])))
            {
                colorListItem(selectedList, slotIndex, Color.FromArgb(220, Color.FromKnownColor(KnownColor.MenuHighlight)), SystemColors.HighlightText);
            }

            //Load appropriate plugins for the selected save
            refreshPluginBindings();

            //Enable certain list items
            enableSelectiveEditItems();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Save Memory Card
            saveCardFunction(mainTabControl.SelectedIndex);
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Open preferences dialog
            editPreferences();
        }

        private void mainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Cleanly close the application
            exitApplication(e);
        }

        private void editHeaderButton_Click(object sender, EventArgs e)
        {
            //Edit header of the selected save
            editSaveHeader();
        }

        private void editIconButton_Click(object sender, EventArgs e)
        {
            //Edit save icon
            editIcon();
        }

        private void editIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Edit icon of the selected save
            editIcon();
        }

        private void editIconToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Edit icon of the selected save
            editIcon();
        }

        private void exportSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Export a save
            exportSaveDialog(false);
        }

        private void exportSaveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Export a save
            exportSaveDialog(false);
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            //Export a save
            exportSaveDialog(false);
        }

        private void importSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Import a save
            importSaveDialog();
        }

        private void importSaveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Import a save
            importSaveDialog();
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            //Import a save
            importSaveDialog();
        }

        private void pasteSaveFromTemporaryBufferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Paste a save from temp buffer to selected slot
            pasteSave();
        }

        private void paseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Paste a save from temp buffer to selected slot
            pasteSave();
        }

        private void tBufToolButton_Click(object sender, EventArgs e)
        {
            //Paste a save from temp buffer to selected slot
            pasteSave();
        }

        private void saveInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Show information about a selected save
            showInformation();
        }

        private void mainTabControl_DragEnter(object sender, DragEventArgs e)
        {
            //Check if dragged data are files
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
        }

        private void editWithPluginToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            //Set the click item
            clickedPlugin[1] = e.ClickedItem.Owner.Items.IndexOf(e.ClickedItem);

            //Set the clicked flag
            clickedPlugin[0] = 1;
        }

        private void editWithPluginToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            //Load clicked plugin if the menu was clicked
            if (clickedPlugin[0] == 1) editWithPlugin(clickedPlugin[1]);

            //Set the clicked flag on false
            clickedPlugin[0] = 0;
        }

        private void mainTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            //Check if the middle mouse button is pressed
            if (e.Button == MouseButtons.Middle)
            {
                Rectangle tabRectangle;

                //Cycle through all available tabs
                for (int i = 0; i < mainTabControl.TabCount; i++)
                {
                    tabRectangle = mainTabControl.GetTabRect(i);

                    //Close the middle clicked tab
                    if (tabRectangle.Contains(e.X, e.Y))closeCard(i,false);
                }
            }
        }

        private void mainTabControl_DragDrop(object sender, DragEventArgs e)
        {
            string[] droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            //Cycle through every dropped file
            foreach (string fileName in droppedFiles)
            {
                //If there are any cards try importing the save first
                if(PScard.Count > 0)
                {
                    int listIndex;
                    int slotNumber;
                    int reqSlots;

                    if(!validityCheck(out listIndex, out slotNumber))
                    {
                        //Something is not right with current selection, try opening file as a card
                        openCard(fileName);
                    }
                    else
                    {
                        if (PScard[listIndex].OpenSingleSave(fileName, slotNumber, out reqSlots) != true) {
                            if (reqSlots > 0)
                            {
                                //Single save was valid but not enough free slots
                                MessageBox.Show(Localization.T("To import this save ") + reqSlots.ToString() + Localization.T(" free slots are required.") + 
                                    Localization.T("\nCreate a new card or remove some existing saves."), appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            else
                            {
                                //Not a single save, try opening as a card
                                openCard(fileName);
                            }
                        }

                        else
                        {
                            //Save was properly imported, show it in the list
                            refreshListView(listIndex, slotNumber);
                            pushHistory(Localization.T("Save imported"), listIndex, prepareIcons(listIndex, slotNumber, false));
                        }
                    }
                }
                else
                {
                    //Basic card open operation
                    openCard(fileName);
                }
            }
        }

        private void compareWithTempBufferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Compare selected save to a temp buffer save
            compareSaveWithTemp();
        }

        private void compareWithTempBufferToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Compare selected save to a temp buffer save
            compareSaveWithTemp();
        }

        private void exportRAWSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Export RAW save
            exportSaveDialog(true);
        }

        private void exportRAWSaveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //Export RAW save
            exportSaveDialog(true);
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Go up a history list
            if(!validityCheck(out int listIndex, out int slotNumber)) return;

            if (historyList[listIndex].SelectedIndices.Count < 1) return;
            int selectedIndex = historyList[listIndex].SelectedIndices[0];

            if (selectedIndex > 0)
            {
                historyList[listIndex].Items[selectedIndex - 1].Selected = true;
                historyList[listIndex].Items[selectedIndex - 1].EnsureVisible();
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Go down a history list
            if (!validityCheck(out int listIndex, out int slotNumber)) return;

            if (historyList[listIndex].SelectedIndices.Count < 1) return;
            int selectedIndex = historyList[listIndex].SelectedIndices[0];

            if (selectedIndex < historyList[listIndex].Items.Count - 1)
            {
                historyList[listIndex].Items[selectedIndex + 1].Selected = true;
                historyList[listIndex].Items[selectedIndex + 1].EnsureVisible();
            }
        }
    }
}
