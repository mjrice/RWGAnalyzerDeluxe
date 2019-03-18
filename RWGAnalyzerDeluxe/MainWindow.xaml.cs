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
using System.IO;

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

        public class CoordType
        {
            public int X;
            public int Y;
            public int Z;

            public CoordType()
            {
                X = 0;
                Y = 0;
                Z = 0;
            }

            public void Parse(string input)
            {
                //CoordType result = new CoordType();
                string[] resultstr;
                char[] charSeparators = new char[] { ',' };
                resultstr = input.Split(charSeparators, StringSplitOptions.None);
                X = Int16.Parse(resultstr[0]);
                Y = Int16.Parse(resultstr[1]);
                if(resultstr.Length>2) Z = Int16.Parse(resultstr[2]);                
            }

            public double distanceTo(CoordType other)
            {
                double d = Math.Sqrt((this.X - other.X) * (this.X - other.X) + (this.Z - other.Z) * (this.Z - other.Z));
                return d;
            }
        };

        public class ColorTableItem
        {
            public Color color;
            public uint count;
        };

        List<ColorTableItem> biomeColors;

        public class WorldMapGridType
        {
            public static int gridFactorMax=28;
            public static int gridFactorMin=4;
            public int gridFactor;
            public int minXLocation;
            public int minZLocation;
            public float gridWidthX;
            public float gridWidthZ;
            public int sizeX;
            public int sizeZ;

            public WorldMapGridType()
            {
                gridFactor = 10;                
            }

            // initialize world map grid with the given world size (X and Z, don't care about Y)
            public void Init(int sx,int sz)
            {
                sizeX = sx;
                sizeZ = sz; // yes 
                gridWidthX = (float)sizeX / gridFactor;
                gridWidthZ = (float)sizeZ / gridFactor; 
                minXLocation = -sizeX / 2;
                minZLocation = -sizeZ / 2;
            }

            public void setGridFactor(int fact)
            {
                gridFactor = fact;
                if (gridFactor < gridFactorMin) gridFactor = gridFactorMin;
                if (gridFactor > gridFactorMax) gridFactor = gridFactorMax;
                gridWidthX = (float)sizeX / gridFactor;
                gridWidthZ = (float)sizeZ / gridFactor;
            }

            // Map a location in world coordinates to a location in grid coordinates
            public void MapLocationToGrid(CoordType location,ref CoordType gridLocation)
            {
                float xbin = (float)(location.X - minXLocation) / gridWidthX;
                gridLocation.X = (int)(xbin);
                float zbin = (float)(location.Z - minZLocation) / gridWidthZ;
                gridLocation.Z = (int)(zbin);
                gridLocation.Z = (gridFactor - 1) - gridLocation.Z; // because the grid "origin" is the lower left corner, not the upper left corner

                if (gridLocation.Z < 0 || gridLocation.Z >= gridFactor || gridLocation.X < 0 || gridLocation.X >= gridFactor)
                {
                    // there was a problem.  maybe the location coordinates we were given were out of the bounds of the map grid.
                    System.Diagnostics.Debug.WriteLine("Runtime error detected in ::MapLocationToGrid()");

                    gridLocation.X = 0;
                    gridLocation.Z = 0;
                }
            }

            public float area()
            {
                return (float)sizeX * (float)sizeZ;
            }
        };

        WorldMapGridType worldGrid;

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

        float waterFillThresholdToShow = 0.5f;
        bool DoShowWaterDistribution = false;
        bool analyzeWater = true;
        string worldFolder = "";
        ConsoleClassType Console;
        FolderBrowserDialog folderPicker;
        XmlDocument xinfoDoc;
        XmlDocument xdoc;
        XmlDocument xwaterdoc;
        int[,] bins;
        Color[,] colorBins;
        ulong [,] waterbins;
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
                    waterbins[x, z] = 0;
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
            for (i = 0; i < worldGrid.gridFactor; i++) { ResultsGrid.ColumnDefinitions.Add(new ColumnDefinition()); }
            for (j = 0; j < worldGrid.gridFactor; j++) { ResultsGrid.RowDefinitions.Add(new RowDefinition()); }

            for (i = 0; i < worldGrid.gridFactor; i++)
            {
                for (j = 0; j < worldGrid.gridFactor; j++)
                {
                    TextBlock txt1 = new TextBlock();
                    txt1.Text = "?";
                    txt1.FontFamily = new FontFamily("Courier New");

                    if (worldGrid.gridFactor < 11) txt1.FontSize = 20;
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

            worldGrid = new WorldMapGridType();
            xinfoDoc  = new XmlDocument();
            xdoc      = new XmlDocument();
            xwaterdoc = new XmlDocument();
            bins      = new int[WorldMapGridType.gridFactorMax, WorldMapGridType.gridFactorMax];
            waterbins = new ulong[WorldMapGridType.gridFactorMax, WorldMapGridType.gridFactorMax];
            colorBins = new Color[WorldMapGridType.gridFactorMax, WorldMapGridType.gridFactorMax];
            namesList = new List<itemType>();
            traderList= new List<string>();
            typecounts       = new int[15];
            typenames        = new string[15];
            typenamesInclude = new bool[15];
            typenames_ignore = new string[4];
            biomeColors = new List<ColorTableItem>();

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
            typenames[13] = "army";
            typenames[14] = "business";
            typenames_ignore[0] = "sign";
            typenames_ignore[1] = "street_light";
            typenames_ignore[2] = "tree_";
            typenames_ignore[3] = "player_start";

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
            tb.Text = "Summary of prefab types:";
            tb.FontWeight = FontWeights.Bold;
            tb.TextAlignment = TextAlignment.Left;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.FontFamily = new FontFamily("Courier New");
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
                cb.FontFamily = new FontFamily("Courier New");
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

            RefreshButton.IsEnabled = true;
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

            if (GetFolder() == System.Windows.Forms.DialogResult.OK)
            {
                SetAllButtonsEnabled(false);
                Analyze();
                SetAllButtonsEnabled(true);
            }
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
            int x, z,i,j;
            var stride = img.PixelWidth * (img.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[stride * img.PixelHeight];
            ulong[,] binRed = new ulong [worldGrid.gridFactor, worldGrid.gridFactor];
            ulong[,] binGreen = new ulong[worldGrid.gridFactor, worldGrid.gridFactor];
            ulong[,] binBlue = new ulong[worldGrid.gridFactor, worldGrid.gridFactor];
            ulong[,] binCount = new ulong[worldGrid.gridFactor, worldGrid.gridFactor];
            
            TextBlockStatus.Text = "Looking at the biomes...";

            biomeColors.Clear();                     

            // copy biome map image to array (this will take a lot of memory most likely)
            img.CopyPixels(pixels, stride, 0);

            System.Windows.Forms.Application.DoEvents();

            worldGrid.Init(img.PixelWidth,img.PixelHeight);

            Color pixelColor = new Color();
            CoordType pixelWorldCoord = new CoordType();
            CoordType gridCoord = new CoordType();
            
            pixelColor.A = 255;
            int pixels_per_grid_z = img.PixelHeight / worldGrid.gridFactor;
            int grid_z = 0;

            for(grid_z=0;grid_z<worldGrid.gridFactor;grid_z++)
            {
                for (z = 0; z < pixels_per_grid_z; z++)
                {
                    pixelWorldCoord.Z = (img.PixelHeight / 2) - ((grid_z*pixels_per_grid_z) + z) - 1;

                    for (x = 0; x < img.PixelWidth; x++)
                    {
                        int index = (grid_z * pixels_per_grid_z + z) * stride + (4 * x);
                        byte blue = pixels[index];
                        byte green = pixels[index + 1];
                        byte red = pixels[index + 2];

                        pixelColor.R = red;
                        pixelColor.G = green;
                        pixelColor.B = blue;

                        bool k = false;

                        for (j = 0; j < biomeColors.Count; j++)
                        {
                            if (biomeColors[j].color.Equals(pixelColor))
                            {
                                k = true;
                                biomeColors[j].count++;
                                break;
                            }
                        }

                        if (k == false)
                        {
                            ColorTableItem newcolor = new ColorTableItem();
                            newcolor.color = pixelColor;
                            newcolor.count = 1;
                            biomeColors.Add(newcolor);
                            System.Diagnostics.Debug.WriteLine("new biome color: " + newcolor.color);
                        }

                        pixelWorldCoord.X = x - (img.PixelWidth / 2) - 1;

                        worldGrid.MapLocationToGrid(pixelWorldCoord, ref gridCoord);

                        binRed[gridCoord.X, grid_z] += red;
                        binGreen[gridCoord.X, grid_z] += green;
                        binBlue[gridCoord.X, grid_z] += blue;
                        binCount[gridCoord.X, grid_z] += 1;
                    }
                }
                // upgrade grid colors as we go
                for (x = 0; x < worldGrid.gridFactor; x++)
                {
                        binRed   [x, grid_z] /= binCount[x, grid_z];
                        binGreen [x, grid_z] /= binCount[x, grid_z];
                        binBlue  [x, grid_z] /= binCount[x, grid_z];
                        colorBins[x, grid_z].R =   (byte)binRed[x, grid_z];
                        colorBins[x, grid_z].G = (byte)binGreen[x, grid_z];
                        colorBins[x, grid_z].B =  (byte)binBlue[x, grid_z];
                        colorBins[x, grid_z].A = 255;
                        SetGridCellBackgroundColor(ResultsGrid, colorBins[x, grid_z], grid_z, x);
                        System.Windows.Forms.Application.DoEvents();
                }                                
            }

            float area = (float)img.PixelWidth * (float)img.PixelHeight;
            float pct;
            /*System.Diagnostics.Debug.WriteLine("the map contained " + biomeColors.Count + " unique colors.");
            foreach (ColorTableItem biomecoloritem in biomeColors)
            {
                pct = (float)biomecoloritem.count / area;
                pct *= 100.0f;
                pct = (int)pct;
                System.Diagnostics.Debug.WriteLine("biome color " + biomecoloritem.color + " appears " + biomecoloritem.count + " times (" + pct + "%)");                 
            }*/

            //
            // dynamically setup biome color analysis grid
            //
            BiomesGrid.Children.Clear();
            BiomesGrid.ColumnDefinitions.Clear();
            BiomesGrid.RowDefinitions.Clear();
            BiomesGrid.ColumnDefinitions.Add(new ColumnDefinition());
            for (j = 0; j <= biomeColors.Count; j++) { BiomesGrid.RowDefinitions.Add(new RowDefinition()); }

            /*Border border = new Border();
            border.BorderThickness = new Thickness(1);
            border.CornerRadius = new CornerRadius(3);
            BiomesGrid.Children.Add(border);*/

            TextBlock txt1 = new TextBlock();
            txt1.Text = "Biome distribution (% of land area):";
            txt1.FontSize = 12;
            txt1.FontWeight = FontWeights.Bold;
            txt1.TextAlignment = TextAlignment.Left;
            txt1.VerticalAlignment = VerticalAlignment.Center;
            txt1.FontFamily = new FontFamily("Courier New");

            Grid.SetColumn(txt1, 0);
            Grid.SetRow(txt1, 0);
            BiomesGrid.Children.Add(txt1);

            for (i = 0; i < biomeColors.Count; i++)
            {
                txt1 = new TextBlock();
                uint colorvalue = (uint)((biomeColors[i].color.R << 8) | biomeColors[i].color.G) << 8 | biomeColors[i].color.B;
                pct = (float)biomeColors[i].count / area;
                pct *= 100.0f;
                pct = (int)pct;

                if (colorvalue == 0xffffff) txt1.Text = "Snow          ";
                else if (colorvalue == 0xffa800) txt1.Text = "Wasteland     ";
                else if (colorvalue == 0xba00ff) { txt1.Text = "Burnt Forest  "; txt1.Foreground = Brushes.White; }
                else if (colorvalue == 0xffe477) txt1.Text = "Desert        ";
                else if (colorvalue == 0x004000) { txt1.Text = "Pine Forest   "; txt1.Foreground = Brushes.White; }
                else txt1.Text = "Other (" + colorvalue.ToString("X") + ")";
                txt1.Text += "\t" + pct.ToString() + "%";

                txt1.FontSize = 12;
                txt1.FontWeight = FontWeights.Bold;
                txt1.TextAlignment = TextAlignment.Left;
                txt1.VerticalAlignment = VerticalAlignment.Center;
                txt1.FontFamily = new FontFamily("Courier New");

                Grid.SetColumn(txt1, 0);
                Grid.SetRow(txt1, i+1);
                txt1.Background = new SolidColorBrush(biomeColors[i].color);
                BiomesGrid.Children.Add(txt1);                            
            }
        }        

        private void RefreshBins(int x,int z)
        {
            if(x>= worldGrid.gridFactor || x<0 || z>= worldGrid.gridFactor || z<0)
            {
                Console.WriteLine("Error during processing.");
                return;
            }
            TextBlock t = GetGridChildElement(ResultsGrid, z, x) as TextBlock;
            if (t != null)
            {
                if (DoShowWaterDistribution)
                {
                    t.Text = waterbins[x, z].ToString();
                }
                else t.Text = bins[x, z].ToString();
            }
        }

        private void Analyze(bool initialize=true)
        {
            bool isknown;
            int counts = 0;
            string fpfn;
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
            CoordType coord = new CoordType();
            CoordType gridCoord = new CoordType();

            System.Diagnostics.Debug.WriteLine("Start ::Analyze()");

            if(initialize) SetupGrid();
            Console.ClearAll();

            if (worldFolder.Length < 2)
            {
                Console.WriteLine("\nNo valid path to analyze.", ConsoleClassType.ResultClass.summary);
                return;
            }

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

                    coord.Parse(size);

                    worldGrid.Init(coord.X, coord.Y);
                }
            }
            Console.WriteLine("The world size is " + worldGrid.sizeX + " by " + worldGrid.sizeZ + ". Each grid cell represents " + worldGrid.gridWidthX + " by " + worldGrid.gridWidthZ + " meters.", ConsoleClassType.ResultClass.summary);

            if (initialize)
            {
                System.Diagnostics.Debug.WriteLine("Loading biome image");
                if (System.IO.File.Exists(worldFolder + "/biomes.png"))
                {
                    FileStream fi = File.OpenRead (worldFolder + "/biomes.png");
                    long filesize = fi.Length;
                    fi.Close();

                    if(filesize < 90000)
                    {
                        Console.WriteLine("biomes.png does not look like a valid PNG... skipping biome analysis",ConsoleClassType.ResultClass.summary);
                    }
                    else 
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
                        thisItemType = 0;
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

                    coord.Parse(location);
                                        
                    // trader locations are also tracked specially here so that we can analyze them separate from other pois                
                    if (nextName.Contains("trader")) traderList.Add(location);

                    //
                    // keep track of the most distant locations in each dimension:
                    //
                    if (coord.X > maxXLocation) maxXLocation = coord.X;
                    if (coord.X < minXLocation) minXLocation = coord.X;

                    if (coord.Y > maxYLocation) maxYLocation = coord.Y;
                    if (coord.Y < minYLocation) minYLocation = coord.Y;

                    if (coord.Z > maxZLocation) maxZLocation = coord.Z;
                    if (coord.Z < minZLocation) minZLocation = coord.Z;

                    //
                    // sort the the prefab into the correct bin on the grid
                    // (only if it is checked on the legend)
                    //           
                    if (thisItemType < 0 || thisItemType >= typenamesInclude.Length)
                    {
                        Console.WriteLine("Error encountered during processing!", ConsoleClassType.ResultClass.summary);
                        return;
                    }
                                        
                    if (typenamesInclude[thisItemType] == true)
                    {
                        worldGrid.MapLocationToGrid(coord,ref gridCoord);
                    
                        bins[gridCoord.X,gridCoord.Z] += 1;
                        RefreshBins(gridCoord.X, gridCoord.Z);
                    }
                    else excludedCount++;
                }
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
                
                // you have to do this if you want underscores to appear, otherwise wpf will suppress them from view
                string fixedname = typenames[i];
                fixedname = fixedname.Replace("_", "__");
                cbox.Content = fixedname + " ".PadRight(14-typenames[i].Length) + " = " + typecounts[i].ToString() + " (" + pct + "%)";
            }

            //
            // analyze trader locations
            //
            if (traderList.Count > 1)
            {
                double aveDistance = 0.0;
                int countdistances = 0;
                CoordType coord2 = new CoordType();
                foreach (string position in traderList)
                {
                    coord.Parse(position);
                    double nearestNeighborDistance = 10000.0;
                    foreach (string position2 in traderList)
                    {
                        if (position.Equals(position2) == false)
                        {
                            coord2.Parse(position2);
                            double distance = coord2.distanceTo(coord);
                            if (distance < nearestNeighborDistance) nearestNeighborDistance = distance;
                        }
                    }
                    aveDistance += nearestNeighborDistance;
                    countdistances++;
                }
                aveDistance /= (double)countdistances;
                aveDistance = (int)(aveDistance + .5);
                Console.WriteLine("On average, each trader is " + aveDistance + " meters away from another trader.", ConsoleClassType.ResultClass.summary);
            }

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
                    coord.Parse(waterPos);                    
                }

                int problems = 0;                
                float waterArea = (waterblockcount * waterBlockSize * waterBlockSize);
                float pctWater = waterArea / worldGrid.area();
                pctWater *= 10000.0f;
                pctWater = (int)pctWater;
                pctWater *= .01f;
                Console.WriteLine("\nIdentified " + waterArea + " square meters of water, which is " + pctWater + "% of the land area.", ConsoleClassType.ResultClass.prefabList);
                //Console.WriteLine("It appears that there are " + buildings_in_water + " buildings located in the water.");
         
                // accumulate (count) how many water blocks are in each results-grid location
                // note that water blocks are each larger than 1 world unit on a side (for some reason they are 8x8)
                foreach (XmlNode node in xwaterdoc.DocumentElement.ChildNodes)
                {
                    string location = node.Attributes["pos"].Value;
                    coord.Parse(location);
                    worldGrid.MapLocationToGrid(coord, ref gridCoord);                                                           
                    waterbins[gridCoord.X,gridCoord.Z] += 1;
                }

                float maxpct = 0f;
                for (z = 0;z<worldGrid.gridFactor;z++)
                {
                    for (x = 0; x < worldGrid.gridFactor; x++)
                    {
                        // the area that is water is the area of a single water block multiplied by how many water blocks are in this grid location
                        float waterarea = waterbins[x, z] * (ulong)waterBlockSize * (ulong)waterBlockSize;
                        pct = waterarea / (worldGrid.gridWidthX * worldGrid.gridWidthZ);

                        if (pct > waterFillThresholdToShow)
                        {
                            colorBins[x, z] = Color.FromRgb(0, 0, 255);
                            SetGridCellBackgroundColor(ResultsGrid, colorBins[x, z], z, x);
                            System.Diagnostics.Debug.WriteLine("grid location [" + x + "," + z + "] marked is more than " + waterFillThresholdToShow + "x100% water");
                        }
                        if (pct > maxpct) maxpct = pct;                        
                        RefreshBins(x, z);
                    }
                }
                System.Diagnostics.Debug.WriteLine("max pct water fill on any grid location was " + maxpct.ToString());
                
            }

            System.Diagnostics.Debug.WriteLine("Finished ::Analyze()");
            TextBlockStatus.Text = "";
            RefreshButton.IsEnabled = false;

        }

        private void SetAllButtonsEnabled(bool b)
        {            
            if(b==false) RefreshButton.IsEnabled = b;
            //if(b==false || (b==true && worldGrid.gridFactor> WorldMapGridType.gridFactorMin)) GridGoLargerButton.IsEnabled = b;
            //if(b==false || (b==true && worldGrid.gridFactor < WorldMapGridType.gridFactorMax)) GridGoSmallerButton.IsEnabled = b;
            ChooseWorldButton.IsEnabled = b;
            SliderGridFactor.IsEnabled = b;
        }

        bool refresh_analyze_map = false;

        private void SliderGridFactor_ChangeValue(object sender,RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("slider value = " + SliderGridFactor.Value);
            if (worldGrid != null)
            {
                worldGrid.gridFactor = (int)SliderGridFactor.Value;
                SetupGrid();
                refresh_analyze_map = true;
                RefreshButton.IsEnabled = true;
            }
            //  worldGrid.setGridFactor((int)SliderGridFactor.Value);
        }

        private void Button_Click_Refresh(object sender, RoutedEventArgs e)
        {
            SetAllButtonsEnabled(false);
            Analyze(refresh_analyze_map);
            SetAllButtonsEnabled(true);
            refresh_analyze_map = false;
        }
    }
}
