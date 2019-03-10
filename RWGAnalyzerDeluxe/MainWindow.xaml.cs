using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
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

namespace RWGAnalyzerDeluxe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class itemType
        {
            public string name;
            public int count;
            public int type;
        };

        public class ConsoleClassType
        {
            public enum ResultClass { summary, prefabList, prefabMap, water, nowhere };
            public TextBlock textBoxSummary;
            public TextBlock textBoxPrefabMap;
            public TextBlock textBoxPrefabList;

            public void setTextBox(ResultClass resultclass, TextBlock tb)
            {
                if (resultclass == ResultClass.summary)
                    textBoxSummary = tb;
                else if (resultclass == ResultClass.prefabList)
                    textBoxPrefabList = tb;
                else if (resultclass == ResultClass.prefabMap)
                    textBoxPrefabMap = tb;
            }

            public void WriteLine(string ln, ResultClass resultclass = ResultClass.nowhere)
            {
                Write(ln, resultclass);
                Write("\n", resultclass);
            }

            public void Write(string txt, ResultClass resultclass = ResultClass.nowhere)
            {
                if (resultclass == ResultClass.summary)
                    textBoxSummary.Text += txt;
                else if (resultclass == ResultClass.prefabList && textBoxPrefabList!=null)
                    textBoxPrefabList.Text += txt;
                else if (resultclass == ResultClass.prefabMap && textBoxPrefabMap!=null)
                    textBoxPrefabMap.Text += txt;
            }

            public void ClearAll()
            {
                if(textBoxPrefabList!=null) textBoxPrefabList.Text = "";
                //textBoxPrefabMap.Text = "";
                if(textBoxSummary!=null) textBoxSummary.Text = "";
            }

        };

        int gridFactor    = 10;
        int gridFactorMax = 16;
        int gridFactorMin = 4;

        string worldFolder = "";
        ConsoleClassType Console;
        FolderBrowserDialog folderPicker;
        XmlDocument xinfoDoc;
        XmlDocument xdoc;
        XmlDocument xwaterdoc;
        int[,] bins;
        Color[,] colorBins;
        List<itemType> namesList;
        List<string> traderList;
        int[] typecounts;
        string[] typenames;
        bool[] typenamesInclude;
        string[] typenames_ignore;

        private void ClearBins()
        {
            double d = Math.Sqrt(bins.Length);
            int di = (int)d;
            
            for (int x = 0; x < di; x++)
            {
                for (int z = 0; z < di; z++)
                {
                    bins[x,z] = 0;
                    RefreshBins(x, z);
                }
            }
        }

        private void SetupGrid()
        {
            int i, j;
            ResultsGrid.Children.Clear();
            ResultsGrid.ColumnDefinitions.Clear();
            ResultsGrid.RowDefinitions.Clear();

            //var B1 = new Border();
            //B1.BorderBrush = Brushes.Black;
            //B1.BorderThickness = new Thickness(1, 1, 1, 1); 
            //LegendGrid.Children.Add(B1);

            //
            // dynamically generate the text blocks inside each cell of the display grid:
            //
            for (i = 0; i < gridFactor; i++) { ResultsGrid.ColumnDefinitions.Add(new ColumnDefinition()); }
            for (j = 0; j < gridFactor; j++) { ResultsGrid.RowDefinitions.Add(new RowDefinition()); }

            for (i = 0; i < gridFactor; i++)
            {
                for (j = 0; j < gridFactor; j++)
                {
                    TextBlock txt1 = new TextBlock();
                    txt1.Text = "?";
                    if (gridFactor < 11) txt1.FontSize = 20;
                    else txt1.FontSize = 12;

                    txt1.FontWeight = FontWeights.Normal;
                    txt1.TextAlignment = TextAlignment.Center;
                    //txt1.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    //txt1.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(txt1, i);
                    Grid.SetRow(txt1, j);
                    
                    ResultsGrid.Children.Add(txt1);
                }
            }
        }

        public MainWindow()
        {
            int i, j;
            InitializeComponent();
            Console = new ConsoleClassType();
            folderPicker = new FolderBrowserDialog();
            //ResultsGrid.ShowGridLines = true;
            folderPicker.ShowNewFolderButton = false;
            Console.setTextBox(ConsoleClassType.ResultClass.summary, TextBlockSummary);
            Console.setTextBox(ConsoleClassType.ResultClass.prefabList, PrefabSummary);
            
            xinfoDoc  = new XmlDocument();
            xdoc      = new XmlDocument();
            xwaterdoc = new XmlDocument();
            bins      = new int[gridFactorMax, gridFactorMax];
            colorBins = new Color[gridFactorMax, gridFactorMax];
            namesList = new List<itemType>();
            traderList= new List<string>();
            typecounts= new int[13];
            typenames = new string[13];
            typenamesInclude = new bool[13];
            typenames_ignore = new string[2];

            typenames[0] = "other";
            typenames[1] = "trader";
            typenames[2] = "survivor_site";
            typenames[3] = "skyscraper";
            typenames[4] = "junkyard";
            typenames[5] = "house";
            typenames[6] = "utility";
            typenames[7] = "store";
            typenames[8] = "cabin";
            typenames[9] = "waste_bldg";
            typenames[10] = "cave";
            typenames[11] = "factory";
            typenames[12] = "field";
            typenames_ignore[0] = "sign";
            typenames_ignore[1] = "street_light";

            for (i = 0; i < typenames.Length; i++) typenamesInclude[i] = true;

            SetupGrid();
                       
            TextBlockSummary.Text = "Choose a world folder to analyze";
            PrefabSummary.Text = "";
            TextBlockStatus.Text = "";
            // dynamically set up the legend grid
            //var B1 = new Border();
            //B1.BorderBrush = Brushes.Black;
            //B1.BorderThickness = new Thickness(1, 1, 1, 1); //You can specify here which borders do you want
            //LegendGrid.Children.Add(B1);

            for (i=0;i<1;i++) { LegendGrid.ColumnDefinitions.Add(new ColumnDefinition()); }

            LegendGrid.RowDefinitions.Add(new RowDefinition());
            for (i=0;i<typenames.Length;i++)
            {
                LegendGrid.RowDefinitions.Add(new RowDefinition());
            }

            TextBlock tb = new TextBlock();
            tb.Text = "Summary of types:";
            Grid.SetColumn(tb, 0);
            Grid.SetRow(tb, 0);
            LegendGrid.Children.Add(tb);

            for (i = 0; i < typenames.Length; i++)
            {
                System.Windows.Controls.CheckBox cb = new System.Windows.Controls.CheckBox();
                cb.Content = typenames[i] + "(?)";
                cb.IsThreeState = false;
                cb.IsChecked = true;
                cb.VerticalAlignment = VerticalAlignment.Center;
                cb.Checked += new RoutedEventHandler(HandleClick);
                cb.Unchecked += new RoutedEventHandler(HandleClick);                            
                Grid.SetColumn(cb, 0);
                Grid.SetRow(cb, i+1);
                LegendGrid.Children.Add(cb);
            }

        }

        public void HandleClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("HandleClick(" + sender + "," + e + ")");
            System.Windows.Controls.CheckBox cb = sender as System.Windows.Controls.CheckBox;
            int k = 0;

            // find which typename was checked or unchecked:
            for(int i=0;i<typenames.Length;i++)
            {
                string cbStr = cb.Content as string;
                if(cbStr.Contains(typenames[i]))
                {
                    k = i;
                    if (cb.IsChecked==true) typenamesInclude[i] = true;
                    else typenamesInclude[i] = false;
                    break;
                }
            }

            Analyze(false);
        }

        private DialogResult GetFolder()
        {            
            DialogResult result = folderPicker.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                worldFolder = folderPicker.SelectedPath;
            }

            return result;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            worldFolder = "";

            if(GetFolder() == System.Windows.Forms.DialogResult.OK)
                Analyze();
        }

        private UIElement GetGridChildElement(Grid grid, int row, int column)
        {
            //Type twanted = Console.textBoxSummary.GetType();

            foreach (UIElement child in grid.Children)
            {
                if (Grid.GetRow(child) == row && Grid.GetColumn(child) == column)
                {
                    return child;
                }
            }
            return null;
        }

        private void SetGridCellBackgroundColor(Grid grid,Color theColor,int row,int column)
        {            
            TextBlock t = GetGridChildElement(grid, row, column) as TextBlock;
            t.Background = new SolidColorBrush(theColor);
        }

        private void ProcessBiomesBitmap(string fp)
        {
            Uri biomesUri = new Uri(fp + "/biomes.png");
            BitmapImage img = new BitmapImage(biomesUri);
            int x, z;
            var stride = img.PixelWidth * (img.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[stride * img.PixelHeight];
            ulong[,] binRed = new ulong [gridFactor, gridFactor];
            ulong[,] binGreen = new ulong[gridFactor, gridFactor];
            ulong[,] binBlue = new ulong[gridFactor, gridFactor];
            ulong[,] binCount = new ulong[gridFactor, gridFactor];

            TextBlockStatus.Text = "Looking at the biomes...";

            // this is very unnecessary
            Random rseq = new Random();
            Color[] randoColor = new Color[16];
            for (x = 0; x < 16; x++)
            {
                byte b = (byte)rseq.Next(256);
                randoColor[x] = Color.FromRgb(b, b, b);
            }

            img.CopyPixels(pixels, stride, 0);

            int binWidth  = img.PixelWidth / gridFactor;
            int binHeight = img.PixelHeight / gridFactor;
            System.Windows.Forms.Application.DoEvents();

            for (z=0;z< img.PixelHeight;z++)
            {
                for(x=0;x<img.PixelWidth;x++)
                {
                    int index = z * stride + 4 * x;
                    byte blue   = pixels[index];
                    byte green = pixels[index + 1];
                    byte red  = pixels[index + 2];

                    float xbin = (float)(x - 0) / binWidth;
                    int xbinInt = (int)(xbin);
                    xbinInt = xbinInt >= gridFactor ? gridFactor-1 : xbinInt;

                    float zbin = (float)(z - 0) / binHeight;
                    int zbinInt = (int)(zbin);
                    zbinInt = zbinInt >= gridFactor ? gridFactor-1 : zbinInt;
                    //zbinInt = 9 - zbinInt;
                    
                      binRed[xbinInt, zbinInt] += red;
                    binGreen[xbinInt, zbinInt] += green;
                     binBlue[xbinInt, zbinInt] += blue;
                    binCount[xbinInt, zbinInt] += 1;                    
                }

                // total waste of cpu cycles here
                int rx = rseq.Next(gridFactor);
                int rz = rseq.Next(gridFactor);
                SetGridCellBackgroundColor(ResultsGrid, randoColor[rseq.Next(16)], rx, rz);
                System.Windows.Forms.Application.DoEvents();
            }

            for(z=0;z< gridFactor; z++)
            {
                for(x=0;x< gridFactor; x++)
                {
                    binRed[x, z] /= binCount[x, z];
                    binGreen[x, z] /= binCount[x, z];
                    binBlue[x, z] /= binCount[x, z];

                    colorBins[x, z].R = (byte)binRed[x, z];
                    colorBins[x, z].G = (byte)binGreen[x, z];
                    colorBins[x, z].B = (byte)binBlue[x, z];
                    colorBins[x, z].A = 255;
                    SetGridCellBackgroundColor(ResultsGrid, colorBins[x,z], z, x);
                    System.Windows.Forms.Application.DoEvents();
                }
            }
        }        

        private void RefreshBins(int x,int z)
        {
            if(x>=gridFactor || x<0 || z>=gridFactor || z<0)
            {
                Console.WriteLine("Error during processing.");
                return;
            }
            TextBlock t = GetGridChildElement(ResultsGrid, z, x) as TextBlock;
            if (t != null) t.Text = bins[x, z].ToString();
        }

        private void Analyze(bool initialize=true)
        {
            bool isknown;
            int counts = 0;
            string fpfn;
            char[] charSeparators = new char[] { ',' };
            string[] result;
            int xSize = 8192;
            int zSize = 8192;
            int x, z;
            int maxXLocation = -100;
            int minXLocation = 100;
            int maxZLocation = -100;
            int minZLocation = 100;
            int maxYLocation = -100;
            int minYLocation = 100;
            int k;
            int thisItemType = -1;
            int excludedCount = 0;

            System.Diagnostics.Debug.WriteLine("Start ::Analyze()");

            if(initialize) SetupGrid();

            if (worldFolder.Length < 2) return;
            Console.ClearAll();
            this.Title = "RWGAnalyzer [" + worldFolder + "]";
            TextBlockStatus.Text = "Working...";
            namesList.Clear();
            traderList.Clear();
            ClearBins();

            //
            // load all the xml documents we need to analyze
            //
            if (initialize)
            {
                System.Diagnostics.Debug.WriteLine("Parsing map_info.xml");
                fpfn = worldFolder + "/map_info.xml";
                xinfoDoc.Load(fpfn);
                System.Diagnostics.Debug.WriteLine("map_info contains " + xinfoDoc.DocumentElement.ChildNodes.Count.ToString() + " child nodes.");

                System.Diagnostics.Debug.WriteLine("Parsing prefabs.xml");
                string fpfn2 = worldFolder + "/prefabs.xml";
                xdoc.Load(fpfn2);
                System.Diagnostics.Debug.WriteLine("prefabs contains " + xdoc.DocumentElement.ChildNodes.Count.ToString() + " child nodes.");
            }

            System.Diagnostics.Debug.WriteLine("reading map info");

            foreach (XmlNode node in xinfoDoc.DocumentElement.ChildNodes)
            {
                string nextName = node.Attributes["name"].Value;
                if (nextName.Equals("HeightMapSize"))
                {
                    string size = node.Attributes["value"].Value;
                    result = size.Split(charSeparators, StringSplitOptions.None);
                    xSize = Int16.Parse(result[0]);
                    zSize = Int16.Parse(result[1]);
                }
            }
            float widthX = (float)xSize / gridFactor;
            float widthZ = (float)zSize / gridFactor; //(float)(maxZLocation-minZLocation+1) * .1f;
            int worldminXLocation = -xSize / 2;
            int worldminZLocation = -zSize / 2;
            Console.WriteLine("The world size is " + xSize + " by " + zSize + ". Each grid cell represents " + widthX + " by " + widthZ + " meters.", ConsoleClassType.ResultClass.summary);

            if (initialize)
            {
                System.Diagnostics.Debug.WriteLine("Loading biome image");
                if (System.IO.File.Exists(worldFolder + "/biomes.png"))
                {
                    ProcessBiomesBitmap(worldFolder);
                }
                else
                {
                    Console.WriteLine("Error: There was no biomes.png in the folder specified.", ConsoleClassType.ResultClass.summary);
                    return;
                }
            }

            //Console.WriteLine("processing input file...");
            // divide the world into a matrix of 10 by 10 blocks and then tally which block each prefab is in, in order to see how spread out or clumpy it is
            // determe how many of each type of prefab exist
            counts = 0;
            System.Diagnostics.Debug.WriteLine("counting types of prefabs");
            TextBlockStatus.Text = "Looking at the POIs...";
            System.Windows.Forms.Application.DoEvents();

            foreach (XmlNode node in xdoc.DocumentElement.ChildNodes)
            {
                // each node should appear like this:  <decoration type="model" name="fastfood_01" position="214,49,-2856" rotation="1" />
                counts++;
                string nextName = node.Attributes["name"].Value;
                string location = node.Attributes["position"].Value;

                isknown = false;
                bool ignorePrefab = false;

                // this step is to weed out prefabs like street signs that don't really represent POIs anyway
                foreach (string dontcare in typenames_ignore)
                {
                    if (nextName.Contains(dontcare))
                        ignorePrefab = true;
                }

                if (ignorePrefab == false)
                {
                    foreach (itemType item in namesList)
                    {
                        if (item.name.Equals(nextName))
                        {
                            isknown = true;
                            item.count++;
                            thisItemType = item.type;
                            break;
                        }
                    }

                    // namesList contains the full name of the prefab that we have identified.  The type names are groups of similar prefabs.
                    // for example, any prefab name that contains the word "field" is grouped together under the legend, even though there
                    // may be several items in namesList (blueberry_field, potato_field, etc.)
                    if (isknown == false)
                    {
                        itemType newitem = new itemType();
                        newitem.name = nextName;
                        newitem.count = 1;
                        newitem.type = 0;
                        for (k = 1; k < typenames.Length; k++)
                        {
                            if (typenames[k].Length > 0) // sanity check
                            {
                                if (nextName.Contains(typenames[k]))
                                {
                                    newitem.type = k;
                                    thisItemType = k;
                                }
                            }
                        }
                        namesList.Add(newitem);
                    }
                }
                
                result = location.Split(charSeparators, StringSplitOptions.None);
                int xLocation = Int16.Parse(result[0]);
                int yLocation = Int16.Parse(result[1]);
                int zLocation = Int16.Parse(result[2]);

                // trader locations are also tracked specially here so that we can analyze them separate from other pois                
                if (nextName.Contains("trader")) 
                {
                    traderList.Add(location);
                }

                //
                // keep track of the most distant locations in each dimension:
                //
                if (xLocation > maxXLocation) maxXLocation = xLocation;
                if (xLocation < minXLocation) minXLocation = xLocation;

                if (yLocation > maxYLocation) maxYLocation = yLocation;
                if (yLocation < minYLocation) minYLocation = yLocation;

                if (zLocation > maxZLocation) maxZLocation = zLocation;
                if (zLocation < minZLocation) minZLocation = zLocation;

                //
                // sort the the prefab into the correct bin on the grid
                // (only if it is checked on the legend)
                //           
                if(thisItemType<0||thisItemType>=typenamesInclude.Length)
                {
                    Console.WriteLine("Error encountered during processing!", ConsoleClassType.ResultClass.summary);
                    return;
                }

                if (typenamesInclude[thisItemType] == true)
                {
                    float xbin = (float)(xLocation - worldminXLocation) / widthX;
                    int xbinInt = (int)(xbin);

                    float zbin = (float)(zLocation - worldminZLocation) / widthZ;
                    int zbinInt = (int)(zbin);
                    zbinInt = (gridFactor - 1) - zbinInt;

                    bins[xbinInt, zbinInt] += 1;
                    RefreshBins(xbinInt, zbinInt);
                }
                else excludedCount++;
                System.Windows.Forms.Application.DoEvents();
            }

            System.Diagnostics.Debug.WriteLine("Excluded " + excludedCount);

            Console.WriteLine("There were " + counts + " prefab instances of " + namesList.Count + " types found in this world.", ConsoleClassType.ResultClass.summary);
            Console.WriteLine("Prefabs will spawn between coordinates (" + minXLocation + "," + minYLocation + "," + minZLocation + ") and (" + maxXLocation + "," + maxYLocation + "," + maxZLocation + ")", ConsoleClassType.ResultClass.summary);

            // sort the list of prefabs (most common ones first)
            namesList.Sort((A, B) => B.count.CompareTo(A.count));

            for (int i = 0; i < typecounts.Length; i++) typecounts[i] = 0;

            // tally count of each prefab type found
            foreach (itemType item in namesList)
            {
                typecounts[item.type] += item.count;
            }

            int mostcommoncount = 0;
            for (int i = 0; i < 10; i++)
            {
                mostcommoncount += namesList[i].count;
            }

            float pct;
            pct = (float)mostcommoncount / counts * 10000.0f;
            int pcti = (int)pct;
            pct = pcti * .01f;

            Console.WriteLine("The 10 most commonly duplicated prefabs account for " + pct + "% of the prefabs and are: ", ConsoleClassType.ResultClass.prefabList);
            Console.WriteLine("Prefab:              \tOccurances:");
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(namesList[i].name.PadRight(21) + "\t" + namesList[i].count, ConsoleClassType.ResultClass.prefabList);
            }
                        
            for (int i = 0; i < typecounts.Length; i++)
            {
                pct = (float)typecounts[i] / counts * 10000.0f;
                pcti = (int)pct;
                pct = pcti * .01f;
                System.Windows.Controls.CheckBox cbox = GetGridChildElement(LegendGrid, i+1, 0) as System.Windows.Controls.CheckBox;
                cbox.Content = typenames[i] + " = " + typecounts[i].ToString().PadLeft(4) + " (" + pct + "%)";
            }

            //
            // analyze trader locations
            //
            double aveDistance = 0.0;
            int countdistances = 0;
            foreach (string position in traderList)
            {
                result = position.Split(charSeparators, StringSplitOptions.None);
                int x1 = Int16.Parse(result[0]);
                int z1 = Int16.Parse(result[2]);
                double nearestNeighborDistance = 10000.0;

                foreach (string position2 in traderList)
                {
                    if (position.Equals(position2) == false)
                    {
                        result = position2.Split(charSeparators, StringSplitOptions.None);
                        int x2 = Int16.Parse(result[0]);
                        int z2 = Int16.Parse(result[2]);

                        double distance = Math.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1));
                        if (distance < nearestNeighborDistance) nearestNeighborDistance = distance;
                    }
                }
                aveDistance += nearestNeighborDistance;
                countdistances++;
            }
            aveDistance /= (double)countdistances;
            aveDistance = (int)(aveDistance + .5);
            Console.WriteLine("On average, each trader is " + aveDistance + " meters away from another trader.", ConsoleClassType.ResultClass.summary);

            bool analyzeWater = true;

            if (analyzeWater)
            {
                TextBlockStatus.Text = "Looking at the Water...";
                System.Windows.Forms.Application.DoEvents();

                System.Diagnostics.Debug.WriteLine("Parsing water_info.xml");
                string fpfn3 = worldFolder + "/water_info.xml";
                xwaterdoc.Load(fpfn3);
                System.Diagnostics.Debug.WriteLine("water_info contains " + xwaterdoc.DocumentElement.ChildNodes.Count.ToString() + " child nodes.");

                //Console.WriteLine("\nAnalyzing bodies of water, this may take a minute...");
                // analyze water
                // <WaterSources>
                // <Water pos="-4092, 39, -3484" minx="-4096" maxx="4096" minz="-4096" maxz="4096"/>
                // The water blocks appear to be 8 meters by 8 meters each
                int waterBlockSize = 8;
                int waterblockcount = 0;
                int buildings_in_water = 0;
                foreach (XmlNode node in xwaterdoc.DocumentElement.ChildNodes)
                {
                    waterblockcount++;
                    string waterPos = node.Attributes["pos"].Value;
                    result = waterPos.Split(charSeparators, StringSplitOptions.None);
                    int waterX = Int16.Parse(result[0]);
                    int waterZ = Int16.Parse(result[2]);
                }

                int problems = 0;
                double worldSquareMeters = xSize * zSize;
                double waterArea = (waterblockcount * waterBlockSize * waterBlockSize);
                double pctWater = waterArea / worldSquareMeters;
                pctWater *= 10000.0;
                pctWater = (int)pctWater;
                pctWater *= .01;
                Console.WriteLine("\nIdentified " + waterArea + " square meters of water, which is " + pctWater + "% of the land area.", ConsoleClassType.ResultClass.prefabList);
                //Console.WriteLine("It appears that there are " + buildings_in_water + " buildings located in the water.");

                /*
                int[,] waterbins = new int[10, 10];
                foreach (XmlNode node in xwaterdoc.DocumentElement.ChildNodes)
                {
                    string location = node.Attributes["pos"].Value;
                    result = location.Split(charSeparators, StringSplitOptions.None);
                    int xLocation = Int16.Parse(result[0]);
                    int zLocation = Int16.Parse(result[2]);

                    float xbin = (float)(xLocation - minXLocation) / widthX;
                    int xbinInt = (int)(xbin);

                    float zbin = (float)(zLocation - minZLocation) / widthZ;
                    int zbinInt = (int)(zbin);

                    if (xbinInt < 0 || xbinInt > 9 || zbinInt < 0 || zbinInt > 9)
                        problems++;
                    else
                        waterbins[xbinInt, zbinInt] += 1;
                }

                Console.WriteLine("In a grid with each block " + widthX + " by " + widthZ + " meters, here is the percentage of each grid block that is covered in water:", ConsoleClassType.ResultClass.prefabMap);
                for (z = 9; z >= 0; z--)
                {
                    Console.Write("\t", ConsoleClassType.ResultClass.prefabMap);
                    for (x = 0; x < 10; x++)
                    {
                        float waterarea = waterbins[x, z] * waterBlockSize * waterBlockSize;
                        pct = waterarea / (widthX * widthZ) * 1000.0f;
                        pct = (int)pct;
                        pct *= 0.1f;
                        string s = pct.ToString("N1");
                        Console.Write(s.PadLeft(4) + "%\t", ConsoleClassType.ResultClass.prefabMap);
                    }
                    Console.Write("\n", ConsoleClassType.ResultClass.prefabMap);
                }
                */
            }

            System.Diagnostics.Debug.WriteLine("Finished ::Analyze()");
            TextBlockStatus.Text = "";

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            gridFactor-=2;
            if (gridFactor < gridFactorMin) gridFactor = gridFactorMin;
            Analyze(true);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            gridFactor+=2;
            if (gridFactor > gridFactorMax) gridFactor = gridFactorMax;
            Analyze(true);
        }
    }
}
