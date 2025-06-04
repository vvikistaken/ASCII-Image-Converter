using Godot;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

public partial class Main : Panel
{
    const string CHAR_LUMINANCE_LIGHT = " #@*+=-:."; // ASCII characters based on luminance
    const string CHAR_LUMINANCE_DARK = " .:-=+*@#"; // Reversed for dark background
    const string CONFIG_FILE_PATH = "user://config.cfg";
    // this took me like 3 hours
    // empty braille (⠀), if i ever need it
    static readonly string[,] BRAILLE = new string[16, 16]
    {
        {"⢀","⠈","⠐","⠘","⠠","⠨","⠰","⠸","⢀","⢈","⢐","⢘","⢠","⢨","⢰","⢸"},
        {"⠁","⠉","⠑","⠙","⠡","⠩","⠱","⠹","⢁","⢉","⢑","⢙","⢡","⢩","⢱","⢹"},
        {"⠂","⠊","⠒","⠚","⠢","⠪","⠲","⠺","⢂","⢊","⢒","⢚","⢢","⢪","⢲","⢺"},
        {"⠃","⠋","⠓","⠛","⠣","⠫","⠳","⠻","⢃","⢋","⢓","⢛","⢣","⢫","⢳","⢻"},
        {"⠄","⠌","⠔","⠜","⠤","⠬","⠴","⠼","⢄","⢌","⢔","⢜","⢤","⢬","⢴","⢼"},
        {"⠅","⠍","⠕","⠝","⠥","⠭","⠵","⠽","⢅","⢍","⢕","⢝","⢥","⢭","⢵","⢽"},
        {"⠆","⠎","⠖","⠞","⠦","⠮","⠶","⠾","⢆","⢎","⢖","⢞","⢦","⢮","⢶","⢾"},
        {"⠇","⠏","⠗","⠟","⠧","⠯","⠷","⠿","⢇","⢏","⢗","⢟","⢧","⢯","⢷","⢿"},
        {"⡀","⡈","⡐","⡘","⡠","⡨","⡰","⡸","⣀","⣈","⣐","⣘","⣠","⣨","⣰","⣸"},
        {"⡁","⡉","⡑","⡙","⡡","⡩","⡱","⡹","⣁","⣉","⣑","⣙","⣡","⣩","⣱","⣹"},
        {"⡂","⡊","⡒","⡚","⡢","⡪","⡲","⡺","⣂","⣊","⣒","⣚","⣢","⣪","⣲","⣺"},
        {"⡃","⡋","⡓","⡛","⡣","⡫","⡳","⡻","⣃","⣋","⣓","⣛","⣣","⣫","⣳","⣻"},
        {"⡄","⡌","⡔","⡜","⡤","⡬","⡴","⡼","⣄","⣌","⣔","⣜","⣤","⣬","⣴","⣼"},
        {"⡅","⡍","⡕","⡝","⡥","⡭","⡵","⡽","⣅","⣍","⣕","⣝","⣥","⣭","⣵","⣽"},
        {"⡆","⡎","⡖","⡞","⡦","⡮","⡶","⡾","⣆","⣎","⣖","⣞","⣦","⣮","⣶","⣾"},
        {"⡇","⡏","⡗","⡟","⡧","⡯","⡷","⡿","⣇","⣏","⣗","⣟","⣧","⣯","⣷","⣿"}
    };
    // kinda not using it, all of it is hardcoded instead
    static readonly int[,] EDGE_DETECTION_KERNEL = new int[3, 3]
    {
        { -1, -1, -1 },
        { -1,  8, -1 },
        { -1, -1, -1 }
    };
    private bool _isDarkMode, _brailleMode, _invertedColors;
    private int _outputFontSize = 16; 
    private Image _currentSourceImage = null;

    private VBoxContainer _MainContainer;
    private HBoxContainer _InputOptionsContainer, _OutputOptionsContainer;
    private LineEdit _FilePathPreview;
    private SpinBox _ScaleSpinBox;
    private Button _UploadButton, _ProcessButton;
    private TextureButton _PathVisibilityToggle;
    private RichTextLabel _ASCIIOutput;
    private FileDialog _FileDialog;
    private CheckButton _DarkModeSwitch, _BrailleSwitch, _InvertedSwitch;
    private ConfigFile _ConfigFile;

    public override void _Ready()
    {
        GD.Print("works!!!");
        GD.Print("for a comeback project, im kinda goated at it\n");

        // node assignments
        _MainContainer = GetNode<VBoxContainer>("MainContainer");
        _FileDialog = GetNode<FileDialog>("FileDialog");

        _InputOptionsContainer = _MainContainer.GetNode<HBoxContainer>("InputOptionsContainer");
        _OutputOptionsContainer = _MainContainer.GetNode<HBoxContainer>("OutputOptionsContainer");
        _ASCIIOutput = _MainContainer.GetNode<ScrollContainer>("ScrollContainer").GetNode<RichTextLabel>("ASCIIOutput");

        _PathVisibilityToggle = _InputOptionsContainer.GetNode<TextureButton>("PathVisibilityToggle");
        _FilePathPreview = _InputOptionsContainer.GetNode<LineEdit>("ScrollContainer/FilePathPreview");
        _UploadButton = _InputOptionsContainer.GetNode<Button>("UploadButton");
        _ProcessButton = _InputOptionsContainer.GetNode<Button>("ProcessButton");

        _ScaleSpinBox = _OutputOptionsContainer.GetNode<SpinBox>("ScaleSpinBox");
        _DarkModeSwitch = _OutputOptionsContainer.GetNode<CheckButton>("DarkModeSwitch");
        _BrailleSwitch = _OutputOptionsContainer.GetNode<CheckButton>("BrailleSwitch");
        _InvertedSwitch = _OutputOptionsContainer.GetNode<CheckButton>("InvertedSwitch");

        // signals
        _PathVisibilityToggle.Toggled += OnPathVisibilityButtonToggled;
        _UploadButton.Pressed += OnUploadButtonPressed; 
        _ProcessButton.Pressed += OnProcessButtonPressed; 
        _FileDialog.FileSelected += OnFileSelected;
        _ScaleSpinBox.ValueChanged += (value) =>
        {
            _ConfigFile.SetValue("Options", "OutputScale", value);
            _ConfigFile.Save(CONFIG_FILE_PATH);
        };
        _DarkModeSwitch.Toggled += (isChecked) =>
        {
            _isDarkMode = isChecked;
            _ConfigFile.SetValue("Options", "DarkMode", isChecked);
            _ConfigFile.Save(CONFIG_FILE_PATH);
        }; 
        _BrailleSwitch.Toggled += (isChecked) =>
        {
            _brailleMode = isChecked;
            _ConfigFile.SetValue("Options", "BrailleMode", isChecked);
            _ConfigFile.Save(CONFIG_FILE_PATH);
        };
        _InvertedSwitch.Toggled += (isChecked) =>
        {
            _invertedColors = isChecked;
            _ConfigFile.SetValue("Options", "InvertedColors", isChecked);
            _ConfigFile.Save(CONFIG_FILE_PATH);
        }; 

        // fetching beginning states
        _ConfigFile = new ConfigFile();
        var error = _ConfigFile.Load(CONFIG_FILE_PATH);
        if (error != Error.Ok)
        {
            GD.PrintErr("Failed to load config file: " + error + "\nCreating a new config file.");
            // let's leave that one out, no reason really
            // _ConfigFile.SetValue("Options", "Path", _FilePathPreview.Text);
            _ConfigFile.SetValue("Options", "PathVisible", _PathVisibilityToggle.ButtonPressed);
            _ConfigFile.SetValue("Options", "OutputScale", _ScaleSpinBox.Value);
            _ConfigFile.SetValue("Options", "DarkMode", _DarkModeSwitch.ButtonPressed);
            _ConfigFile.SetValue("Options", "BrailleMode", _BrailleSwitch.ButtonPressed);
            _ConfigFile.SetValue("Options", "InvertedColors", _InvertedSwitch.ButtonPressed);

            _ConfigFile.Save(CONFIG_FILE_PATH);
        }
        _PathVisibilityToggle.ButtonPressed = (bool)_ConfigFile.GetValue("Options", "PathVisible", true);
        _ScaleSpinBox.Value = (float)_ConfigFile.GetValue("Options", "OutputScale", 1.0f);
        _DarkModeSwitch.ButtonPressed = (bool)_ConfigFile.GetValue("Options", "DarkMode", true);
        _BrailleSwitch.ButtonPressed = (bool)_ConfigFile.GetValue("Options", "BrailleMode", false);
        _InvertedSwitch.ButtonPressed = (bool)_ConfigFile.GetValue("Options", "InvertedColors", false);

        // setting beggining states
        _isDarkMode = _DarkModeSwitch.ButtonPressed;
        _brailleMode = _BrailleSwitch.ButtonPressed;
        _invertedColors = _InvertedSwitch.ButtonPressed;
        UpdateDarkMode();
        
    }
    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("SizeUp")) _outputFontSize += 1;
        if (Input.IsActionJustPressed("SizeDown")) _outputFontSize -= 1;
        if (_outputFontSize < 4) _outputFontSize = 4;
        if (_outputFontSize > 64) _outputFontSize = 64;

        _ASCIIOutput.AddThemeFontSizeOverride("normal_font_size", _outputFontSize);
    }
    //
    //
    // singal handlers
    public void OnUploadButtonPressed()
    {
        _FileDialog.Visible = true;
    }
    public void OnFileSelected(string filePath)
    {
        GD.Print("File selected: " + filePath);
        _FilePathPreview.Text = filePath;
    }
    public void OnProcessButtonPressed()
    {
        ProcessImage();
    }
    public void NumberInputHandler(LineEdit input, string text)
    {
        input.Text = text
            .Where(c => char.IsDigit(c) || c == ' ')
            .Aggregate("", (current, c) => current + c);
        input.CaretColumn = input.Text.Length; // Move caret to the end
        // thanks copilot XOXO
    }
    public void OnPathVisibilityButtonToggled(bool toggled_on)
    {
        _FilePathPreview.Secret = !toggled_on;
        _FilePathPreview.PlaceholderText = toggled_on ? "a file path . . ." : "a hidden file path . . .";

        _ConfigFile.SetValue("Options", "PathVisible", _PathVisibilityToggle.ButtonPressed);
        _ConfigFile.Save(CONFIG_FILE_PATH);
    }
    //
    //
    // custom methods
    public void LoadImageFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            GD.PrintErr("No file path provided.");
            _FilePathPreview.PlaceholderText = "No file selected.";
            _FilePathPreview.Text = string.Empty;
            return;
        }
        if (!System.IO.File.Exists(filePath))
        {
            GD.PrintErr("File does not exist: " + filePath);
            _FilePathPreview.PlaceholderText = "File does not exist.";
            _FilePathPreview.Text = string.Empty;
            return;
        }
        var tempSourceImage = Image.LoadFromFile(filePath);
        if (tempSourceImage == null)
        {
            GD.PrintErr("Failed to load image from file: " + filePath);
            _FilePathPreview.PlaceholderText = "Failed to load image.";
            _FilePathPreview.Text = string.Empty;
            return;
        }
        // if everything succeeds
        _currentSourceImage = Image.LoadFromFile(filePath);
        _FilePathPreview.PlaceholderText = _PathVisibilityToggle.ButtonPressed ? "a file path . . ." : "a hidden file path . . .";
    }
    public void UpdateDarkMode()
    {
        if (_isDarkMode)
        {
            _ASCIIOutput.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f) });
            _ASCIIOutput.AddThemeColorOverride("default_color", new Color(0.9f, 0.9f, 0.9f));
        }
        else
        {
            _ASCIIOutput.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(0.9f, 0.9f, 0.9f) });
            _ASCIIOutput.AddThemeColorOverride("default_color", new Color(0.1f, 0.1f, 0.1f));
        }
    }
    public void ProcessImage()
    {
        _ASCIIOutput.Clear();
        LoadImageFromFile(_FilePathPreview.Text);

        var processedImage = (Image)_currentSourceImage.Duplicate();
        processedImage.Resize(
            (int)(_currentSourceImage.GetWidth() * (_ScaleSpinBox.Value/100)),
            (int)(_currentSourceImage.GetHeight() * (_ScaleSpinBox.Value/100) * (_brailleMode ? 1 : 0.5)),
            Image.Interpolation.Bilinear
        );

        UpdateDarkMode();

        if (!_brailleMode) GenerateASCIIImage(processedImage);
        else GenerateBrailleImage(processedImage);
    }
    public void GenerateASCIIImage(Image inputImage)
    {
        for (int y = 0; y < inputImage.GetHeight(); y++)
        {
            for (int x = 0; x < inputImage.GetWidth(); x++)
            {
                var pixel = inputImage.GetPixel(x, y);
                float pxLuminance = pixel.Luminance;
                // some crazy math that copilot came up with
                char ch;
                if(!_invertedColors)
                    ch = CHAR_LUMINANCE_DARK[(int)(pxLuminance * (CHAR_LUMINANCE_DARK.Length - 1))];
                else
                    ch = CHAR_LUMINANCE_LIGHT[(int)(pxLuminance * (CHAR_LUMINANCE_LIGHT.Length - 1))];

                _ASCIIOutput.AppendText(ch.ToString());
            }
            _ASCIIOutput.AppendText("\n");
        }
    }
    public void GenerateBrailleImage(Image inputImage)
    {
        var tempInputImage = (Image)inputImage.Duplicate();
        for (int y = 0; y < inputImage.GetHeight(); y++)
        {
            for (int x = 0; x < inputImage.GetWidth(); x++)
            {
                var curPixel = inputImage.GetPixel(x, y)*8;
                
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (x + dx < 0 ||
                            x + dx >= inputImage.GetWidth() ||
                            y + dy < 0 ||
                            y + dy >= inputImage.GetHeight() ||
                            (dx == 0 && dy == 0)
                        ) continue;
                        var tempPixel = inputImage.GetPixel(x + dx, y + dy);
                        tempPixel.R *= -1;
                        tempPixel.G *= -1;
                        tempPixel.B *= -1;

                        curPixel += tempPixel;
                    }
                }
                tempInputImage.SetPixel(x, y, curPixel);
            }
        }
        inputImage = (Image)tempInputImage.Duplicate();
        inputImage.Convert(Image.Format.La8);

        // improves visibility for debug
        var debugImage = (Image)inputImage.Duplicate();
        debugImage.Convert(Image.Format.L8);
        GetNode<TextureRect>("DEBUG ImageOutput").Texture = ImageTexture.CreateFromImage(debugImage);
        
        for (int y = 0; y < inputImage.GetHeight(); y += 4)
        {
            for (int x = 0; x < inputImage.GetWidth(); x += 2)
            {
                int leftSide = 0, rightSide = 0;

                for (int dx = 0; dx < 2; dx++)
                {
                    if (x + dx > inputImage.GetWidth()) return; // Prevent out of bounds
                    for (int dy = 0; dy < 4; dy++)
                    {
                        if (y + dy > inputImage.GetHeight()) return;
                        var pixel = inputImage.GetPixel(x + dx, y + dy);

                        if ((pixel.Luminance > 0.1f && !_invertedColors) ||
                            (pixel.Luminance < 0.1f && _invertedColors && pixel.A > 0)) // Threshold for dark pixels
                        {
                            // Calculate the braille character index
                            int index = (int)Math.Pow(2, dy);
                            if (dx == 0) leftSide += index;
                            else rightSide += index;
                        }
                    }
                }

                _ASCIIOutput.AppendText(BRAILLE[leftSide, rightSide]);
            }
            _ASCIIOutput.AppendText("\n");
        }
    }
}
