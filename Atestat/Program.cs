using Gtk;
using Cairo;

using Atestat.Scene;
using Gdk;
using Window = Gtk.Window;

namespace Atestat;

class Program
{
    public static uint FRAME_RATE = 120;
        
    static void Main()
    {
        Application.Init();
        ProgramWindow program = new ProgramWindow();
        program.ShowAll();
        Application.Run();
    }
}

internal class ProgramWindow : Window
{
    private Scene.Scene scene;
    private bool movingCamera = false;
    private bool draggingComponent = false;
    
    private Component? draggedComponent = null;
    private (double x, double y) initialDragPosition;
    
    private bool draggingWire = false;
    private Pin? draggedPin = null; private int pinIndex = 0;
    private int draggedState = 0; // 0 = none, 1 = input, 2 = output
    
    public ProgramWindow() : base("Logic Circuit Simulator")
    {
        SetDefaultSize(800, 600);
        DeleteEvent += delegate
        {
            Application.Quit();
        };
        
        scene = new Scene.Scene();

        var drawingArea = new DrawingArea();
        drawingArea.Drawn += (s, args) =>
        {
            scene.OnDraw(args.Cr, Allocation.Width, Allocation.Height, draggedPin, draggedState);
        };
        
        Add(drawingArea);
        
        ButtonPressEvent += OnButtonPress;
        ButtonReleaseEvent += OnButtonRelease;
        MotionNotifyEvent += (s, args) =>
        {
            OnMotion(drawingArea, args);
        };
        
        GLib.Timeout.Add(1000 / Program.FRAME_RATE, () =>
        {
            scene.OnUpdate();
            drawingArea.QueueDraw();

            int x, y; ModifierType mask = ModifierType.None;
            Window.GetPointer(out x, out y, out mask);
            
            currentX = x;
            currentY = y;

            if (chars.Count > 0)
            {
                chars.RemoveAt(0);
            }
            return true;
        });

        KeyPressEvent += OnKeyPress;
        
        Display display = Display.Default;
        
        Gdk.Cursor cursor = new Cursor(display, CursorType.Hand1);
    }
    
    bool press = false;
    private void OnButtonPress(object sender, ButtonPressEventArgs args)
    {
        if (press)
        {
            press = false;
            return;
        }
        press = true;

        if (args.Event.Button == 1)
        {
            if (dropdownOpen && scene.selectedComponent != "")
            {
                if (scene.selectedComponent == "Import Custom Chip")
                {
                    CloseDropdown();
                    LoadCustomChip();
                }
                else if (scene.selectedComponent == "Export Custom Chip")
                {
                    CloseDropdown();
                    SaveCustomChip();
                }
                else if (scene.selectedComponent == "Import Schematic")
                {
                    CloseDropdown();
                    LoadSchematic();
                }
                else if (scene.selectedComponent == "Export Schematic")
                {
                    CloseDropdown();
                    SaveSchematic();
                }
                else
                {
                    Type componentType = components[scene.selected][scene.selectedComponent];
                    if (componentType != null)
                    {
                        Component component = (Component)Activator.CreateInstance(componentType);
                        var (x, y) = scene.ScreenToWorld(mouseX, mouseY);
                        component.x = x;
                        component.y = y;
                        scene.AddComponent(component);
                    }

                    CloseDropdown();
                    return;
                }
            }
        }
        
        mouseX = args.Event.X;
        mouseY = args.Event.Y;
        
        // Dragging
        if (args.Event.Button == 2)
        {
            CloseDropdown();
            movingCamera = true;
        }
        
        // Selecting a component
        if (args.Event.Button == 1)
        {
            CloseDropdown();
            
            var (x, y) = scene.ScreenToWorld(args.Event.X, args.Event.Y);
            Component? selectedComponent = scene.GetClickedComponent(args.Event.X, args.Event.Y);
            
            if (selectedComponent != null && draggedComponent == null)
            {
                draggingComponent = true;
                draggedComponent = selectedComponent;
                initialDragPosition = (selectedComponent.x - x, selectedComponent.y - y);
            }
            if (selectedComponent != null && selectedComponent is Atestat.Scene.Button button)
            {
                button.Press();
            }
        }
        
        draggingWire = false;
        draggedPin = null;
        draggedState = 0;
        if (args.Event.Button == 3)
        {
            pinIndex = -1;
            
            var (x, y) = scene.ScreenToWorld(args.Event.X, args.Event.Y);
            Component? selectedComponent = scene.GetClickedComponent(args.Event.X, args.Event.Y);
            
            if (selectedComponent != null)
            {
                CloseDropdown();
                
                draggingWire = true;
                
                var (sx, sy, w, h) = scene.GetComponentBounds(selectedComponent);
                
                draggedComponent = selectedComponent;
                
                bool asInput = (x < (sx + w) / 2);
                
                if (selectedComponent.Inputs.Count > 0 && selectedComponent.Outputs.Count == 0)
                    asInput = true;
                else if (selectedComponent.Inputs.Count == 0 && selectedComponent.Outputs.Count > 0)
                    asInput = false;

                if (asInput)
                {
                    double pinSpacing = (h - sy) / (selectedComponent.Inputs.Count + 1);
                    
                    double closestDistance = double.MaxValue;
                    Pin? closestPin = null!;
                    for (int i = 0; i < selectedComponent.Inputs.Count; i++)
                    {
                        if (Math.Abs(sy + pinSpacing * (i + 1) - y) < closestDistance)
                        {
                            closestPin = selectedComponent.Inputs[i];
                            pinIndex = i;
                            closestDistance = Math.Abs(sy + pinSpacing * (i + 1) - y);
                        }
                    }
                    
                    draggedPin = closestPin;
                    draggedState = 1;
                }
                else
                {
                    double pinSpacing = (h - sy) / (selectedComponent.Outputs.Count + 1);
                    
                    double closestDistance = double.MaxValue;
                    Pin? closestPin = null!;
                    for (int i = 0; i < selectedComponent.Outputs.Count; i++)
                    {
                        if (Math.Abs(sy + pinSpacing * (i + 1) - y) < closestDistance)
                        {
                            closestPin = selectedComponent.Outputs[i];
                            pinIndex = i;
                            closestDistance = Math.Abs(sy + pinSpacing * (i + 1) - y);
                        }
                    }
                    
                    draggedPin = closestPin;
                    draggedState = 2;
                }
            }
            else
            {
                OpenDropdown();
            }
        }
    }
    
    private void OnButtonRelease(object sender, ButtonReleaseEventArgs args)
    {
        if (args.Event.Button == 2)
        {
            movingCamera = false;
        }
        
        if (args.Event.Button == 1)
        {
            draggingComponent = false;
            draggedComponent = null;
        }
        
        if (args.Event.Button == 3)
        {
            Component? selectedComponent = scene.GetClickedComponent(args.Event.X, args.Event.Y);

            if (draggingWire && selectedComponent != null)
            {
                var (x, y) = scene.ScreenToWorld(args.Event.X, args.Event.Y);
                
                var (sx, sy, w, h) = scene.GetComponentBounds(selectedComponent);

                bool asInput = (x < (sx + w) / 2);
                
                if (selectedComponent.Inputs.Count > 0 && selectedComponent.Outputs.Count == 0)
                    asInput = true;
                else if (selectedComponent.Inputs.Count == 0 && selectedComponent.Outputs.Count > 0)
                    asInput = false;
                
                if (asInput)
                {
                    if (selectedComponent.Inputs.Count > 0)
                    {
                        double pinSpacing = (h - sy) / (selectedComponent.Inputs.Count + 1);
                        double closestDistance = double.MaxValue;
                        int closestIndex = -1;
                        for (int i = 0; i < selectedComponent.Inputs.Count; i++)
                        {
                            if (Math.Abs(sy + pinSpacing * (i + 1) - y) < closestDistance)
                            {
                                closestIndex = i;
                                closestDistance = Math.Abs(sy + pinSpacing * (i + 1) - y);
                            }
                        }

                        if(selectedComponent != draggedComponent)
                            selectedComponent.Inputs[closestIndex] = draggedPin!;
                    }
                }
                else
                {
                    if (selectedComponent.Outputs.Count > 0)
                    {
                        double pinSpacing = (h - sy) / (selectedComponent.Outputs.Count + 1);
                        double closestDistance = double.MaxValue;
                        int closestIndex = -1;
                        for (int i = 0; i < selectedComponent.Outputs.Count; i++)
                        {
                            if (Math.Abs(sy + pinSpacing * (i + 1) - y) < closestDistance)
                            {
                                closestIndex = i;
                                closestDistance = Math.Abs(sy + pinSpacing * (i + 1) - y);
                            }
                        }

                        if (pinIndex != -1 && closestIndex != -1 && pinIndex < draggedComponent!.Inputs.Count && closestIndex < selectedComponent.Outputs.Count && selectedComponent != draggedComponent)
                            draggedComponent!.Inputs[pinIndex] = selectedComponent.Outputs[closestIndex];
                    }
                }
            }
            
            draggingWire = false;
            draggedPin = null;
            draggedState = 0;
        }
    }
    
    public static double mouseX = 0, mouseY = 0;
    public static double currentX = 0, currentY = 0;
    private void OnMotion(DrawingArea sender, MotionNotifyEventArgs args)
    {
        currentX = args.Event.X;
        currentY = args.Event.Y;
        
        if (movingCamera)
        {
            double sensitivity = 0.5;
            double deltaX = mouseX - args.Event.X;
            double deltaY = mouseY - args.Event.Y;
            
            scene.camera.x -= deltaX * sensitivity * scene.camera.zoom;
            scene.camera.y -= deltaY * sensitivity * scene.camera.zoom;
            
            mouseX = args.Event.X;
            mouseY = args.Event.Y;
        }
        
        if (draggingComponent)
        {
            var (x, y) = scene.ScreenToWorld(args.Event.X, args.Event.Y);
            draggedComponent!.x = x + initialDragPosition.x;
            draggedComponent!.y = y + initialDragPosition.y;
        }
    }

    public static List<char> chars = new();
    private void OnKeyPress(object sender, KeyPressEventArgs args)
    {
        if (dropdownOpen)
        {
            return;
        }
        
        mouseX = currentX;
        mouseY = currentY;

        if (args.Event.Key == Gdk.Key.F1)
        {
            SaveCustomChip();
        }
        
        if (args.Event.Key == Gdk.Key.F2)
        {
            LoadCustomChip();
        }
        
        if (args.Event.Key == Gdk.Key.F3)
        {
            SaveSchematic();
        }
        
        if (args.Event.Key == Gdk.Key.F4)
        {
            LoadSchematic();
        }
        
        if (args.Event.Key == Gdk.Key.F12)
        {
            scene = new Scene.Scene();
        }
        
        if (args.Event.Key == Gdk.Key.Delete)
        {
            Component? selectedComponent = scene.GetClickedComponent(mouseX, mouseY);
            
            if (selectedComponent != null)
            {
                scene.RemoveComponent(selectedComponent);
            }
        }
        
        if (args.Event.KeyValue < 32 || args.Event.KeyValue > 126) 
            return;
        chars.Add((char) args.Event.KeyValue);
    }

    public void SaveCustomChip()
    {
        List<Component> components = new List<Component>(scene.GetComponents());
            
        Gtk.FileChooserDialog fileChooser = new("Save custom chip", this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
        fileChooser.SetCurrentFolder("custom_components");
        fileChooser.Filter = new FileFilter();
        fileChooser.Filter.AddPattern("*.lcc");
        fileChooser.Filter.Name = "Custom Chip (*.lcc)";
            
        if (fileChooser.Run() == (int) ResponseType.Accept)
        {
            string filename = fileChooser.Filename;
                
            string chipName = filename.Split("/").Last().Split(".").First();
                
            CompoundComponent compoundComponent = new CompoundComponent(components, chipName);
                
            Scene.Scene.SaveCustomComponent(compoundComponent, filename);
        }
            
        fileChooser.Destroy();
    }

    public void LoadCustomChip()
    {
        Gtk.FileChooserDialog fileChooser = new("Select a custom chip to load", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        fileChooser.SetCurrentFolder("custom_components");
        fileChooser.Filter = new FileFilter();
        fileChooser.Filter.AddPattern("*.lcc");
        fileChooser.Filter.Name = "Custom Chip (*.lcc)";
            
        if (fileChooser.Run() == (int) ResponseType.Accept)
        {
            string filename = fileChooser.Filename;
                
            CompoundComponent? compoundComponent = Scene.Scene.LoadCustomComponent(filename);
                
            if (compoundComponent != null)
            {
                scene.AddComponent(compoundComponent, mouseX, mouseY);
            }
        }
            
        fileChooser.Destroy();
    }

    public void SaveSchematic()
    {
        Gtk.FileChooserDialog fileChooser = new("Save schematic", this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
        fileChooser.SetCurrentFolder("schematics");
        fileChooser.Filter = new FileFilter();
        fileChooser.Filter.AddPattern("*.schem");
        fileChooser.Filter.Name = "Schematic (*.schem)";
        
        if (fileChooser.Run() == (int) ResponseType.Accept)
        {
            string filename = fileChooser.Filename;

            string schematic = scene.Serialize();
            
            System.IO.File.WriteAllText(filename, schematic);
        }
        
        fileChooser.Destroy();
    }
    
    public void LoadSchematic()
    {
        Gtk.FileChooserDialog fileChooser = new("Select a schematic to load", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        fileChooser.SetCurrentFolder("schematics");
        fileChooser.Filter = new FileFilter();
        fileChooser.Filter.AddPattern("*.schem");
        fileChooser.Filter.Name = "Schematic (*.schem)";
        
        if (fileChooser.Run() == (int) ResponseType.Accept)
        {
            string filename = fileChooser.Filename;
            
            string schematic = System.IO.File.ReadAllText(filename);

            Scene.Scene scn = Scene.Scene.Deserialize(schematic);
            
            scene = scn;
        }
        
        fileChooser.Destroy();
    }
    
    public static bool dropdownOpen = false;
    
    public static List<Dictionary<string, Type>> components = new()
    {
        new Dictionary<string, Type> // Input/Output
        {
            {"Add Input", typeof(Scene.Button)},
            {"Add Output", typeof(Light)},
            {"Add 1Hz Clock", typeof(Clock)},
        },
        new Dictionary<string, Type> // Logic Gates
        {
            {"Add And Gate", typeof(AndGate)},
            {"Add Or Gate", typeof(OrGate)},
            {"Add Not Gate", typeof(NotGate)},
            {"Add Xor Gate", typeof(XorGate)},
            {"Add Nand Gate", typeof(NandGate)},
            {"Add Nor Gate", typeof(NorGate)},
            {"Add Xnor Gate", typeof(XnorGate)}
        },
        new Dictionary<string, Type> // Modules
        {
            {"Add Modulator", typeof(Modulator)},
            {"Add Keyboard", typeof(KeyboardModule)},
            {"Add 4-bit Counter", typeof(Counter4)},
            {"Add 8-bit Counter", typeof(Counter8)},
            {"Add Snake CPU", typeof(Snake)}
        },
        new Dictionary<string, Type> // Displays
        {
            {"Add 7SEG Driver", typeof(SevenSegmentDriver)},
            {"Add 7SEG Display", typeof(SevenSegmentDisplay)},
            {"Add 16x2 LCD", typeof(LCDDisplay)},
            {"Add 16x16 LCD", typeof(LCDDisplayBig)}
        },
        new Dictionary<string, Type>() // Custom Components
        {
            {"Import Custom Chip", null},
            {"Export Custom Chip", null},
            {"Import Schematic", null},
            {"Export Schematic", null}
        }
    };

    public void OpenDropdown()
    {
        dropdownOpen = true;
        scene.selected = -1;
        scene.selectedComponent = "";
    }
    
    public void CloseDropdown()
    {
        dropdownOpen = false;
    }
}