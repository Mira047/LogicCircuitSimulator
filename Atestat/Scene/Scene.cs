namespace Atestat.Scene;

using Cairo;
using Gtk;
using Newtonsoft.Json;

public class Scene
{
    public struct Camera
    {
        public double x, y, zoom;
    };
    
    public Camera camera;
    
    private List<Component> components;
    
    public Scene()
    {
        camera = new Camera { x = 0, y = 0, zoom = 1 };
        
        components = new List<Component>();
    }
    
    public void AddComponent(Component component)
    {
        components.Add(component);
    }

    public void AddComponent(Component component, double x, double y)
    {
        component.x = x;
        component.y = y;
        
        components.Add(component);
    }

    public static CompoundComponent? LoadCustomComponent(String path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        
        string json = File.ReadAllText(path);

        CompoundComponent component = CompoundComponent.deserialize(json);
        
        return component;
    }

    public static void SaveCustomComponent(CompoundComponent component, String path)
    {
        string json = component.serialize();
        
        File.WriteAllText(path, json);
    }
    
    public void RemoveComponent(Component component)
    {
        components.Remove(component);
        
        foreach (var c in components)
        {
            for (int i = 0; i < c.Inputs.Count; i++)
            {
                if (c.Inputs[i].Component == component)
                {
                    c.Inputs[i].Component = null;
                    c.Inputs[i].State = false;
                }
            }
        }
    }
    
    public List<Component> GetComponents()
    {
        return components;
    }

    public void OnUpdate()
    {
        foreach (var component in components)
        {
            component.Update();
        }
    }
    
    public void OnDraw(Context cr, int width, int height, Pin? draggedPin, int draggedState)
    {
        cr.SetSourceRGB(0.2, 0.2, 0.2);
        cr.Rectangle(0, 0, width, height);
        cr.Fill();
    
        double startX = camera.x;
        double startY = camera.y;
        
        cr.SetSourceRGB(0.3, 0.3, 0.3);
        cr.LineWidth = 1;

        int lineOffset = 40;
        
        // vertical lines
        for (double x = startX % lineOffset; x < width; x += lineOffset * camera.zoom)
        {
            cr.MoveTo(x, 0);
            cr.LineTo(x, height);
            cr.Stroke();
        }
        
        // horizontal lines
        for (double y = startY % lineOffset; y < height; y += lineOffset * camera.zoom)
        {
            cr.MoveTo(0, y);
            cr.LineTo(width, y);
            cr.Stroke();
        }
        
        // components
        foreach (var component in components)
        {
            DrawComponent(cr, component, draggedPin, draggedState);
        }
        
        // UI
        if (ProgramWindow.dropdownOpen)
        {
            var (x, y, w, h) = (ProgramWindow.mouseX, ProgramWindow.mouseY, 150.0, 180.0);
            double dropdownWidth = 150;
            
            bool drawRight = x + w + dropdownWidth >= width;
            bool drawUp = y + h >= height;

            if (drawRight)
            {
                x -= 150;
            }
            
            if (drawUp)
            {
                y -= 180;
            }
            
            cr.SetSourceRGB(0.07, 0.07, 0.07);
            cr.Rectangle(x, y, w, h);
            cr.Fill();
            
            cr.SetSourceRGB(1, 1, 1);
            cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Normal);
            cr.SetFontSize(14);
            
            string[] options = new string[] { "ADD INPUT/OUTPUT", "ADD LOGIC GATE", "ADD MODULE", "ADD DISPLAY", "CUSTOM" };

            int mx = (int)ProgramWindow.currentX; int my = (int)ProgramWindow.currentY;

            double textPadding = 20;
            double totalHeight = options.Length * (cr.FontExtents.Height + textPadding) - textPadding;
            
            double yy = y + (h - totalHeight) / 2 + cr.FontExtents.Ascent;

            bool ok = false;
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                
                int len = ProgramWindow.components[i].Count;
                
                double dropdownHeight = len * (cr.FontExtents.Height + textPadding);
                double dropdownX = drawRight ? x - dropdownWidth : x + w;
                
                if ((mx >= x && mx <= x + w && my >= yy - cr.FontExtents.Ascent - textPadding / 2 && my <= yy + cr.FontExtents.Descent + textPadding / 2) || (selected == i && mx >= dropdownX && mx <= dropdownX + dropdownWidth && my >= yy - cr.FontExtents.Ascent - textPadding / 2 && my <= yy - cr.FontExtents.Ascent - textPadding / 2 + dropdownHeight))
                {
                    selected = i;
                    ok = true;
                    cr.SetSourceRGB(0.12, 0.12, 0.12);
                    cr.Rectangle(x, yy - cr.FontExtents.Ascent - textPadding / 2, w, cr.FontExtents.Height + textPadding);
                    cr.Fill();
                    cr.SetSourceRGB(1, 1, 1);

                    cr.SetSourceRGB(0.07, 0.07, 0.07);
                    
                    cr.Rectangle(dropdownX, yy - cr.FontExtents.Ascent - textPadding / 2, dropdownWidth, dropdownHeight);
                    cr.Fill();
                    
                    cr.SetSourceRGB(1, 1, 1);
                    cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Normal);
                    cr.SetFontSize(14);
                    
                    double dyy = yy;
                    
                    for (int j = 0; j < len; j++)
                    {
                        var key = ProgramWindow.components[i].ElementAt(j).Key;
                        
                        var value = ProgramWindow.components[i].ElementAt(j).Value;
                        
                        if (mx >= dropdownX && mx <= dropdownX + dropdownWidth && my >= dyy - cr.FontExtents.Ascent - textPadding / 2 && my <= dyy - cr.FontExtents.Ascent - textPadding / 2 + cr.FontExtents.Height + textPadding)
                        {
                            cr.SetSourceRGB(0.12, 0.12, 0.12);
                            cr.Rectangle(dropdownX, dyy - cr.FontExtents.Ascent - textPadding / 2, dropdownWidth, cr.FontExtents.Height + textPadding);
                            cr.Fill();
                            cr.SetSourceRGB(1, 1, 1);
                            
                            selectedComponent = key;
                            ok = true;
                        }
                        
                        cr.MoveTo(dropdownX + 10, dyy);
                        cr.ShowText(key);
                        
                        dyy += cr.FontExtents.Height + textPadding;
                    }
                }

                double xx = x + h / 25;

                cr.MoveTo(xx, yy);
                cr.ShowText(option);

                yy += cr.FontExtents.Height + textPadding;
            }
            
            if (!ok)
            {
                selectedComponent = "";
                selected = -1;
            }
        }
        
        var (wx, wy) = ScreenToWorld(ProgramWindow.currentX, ProgramWindow.currentY);
        
        cr.SetSourceRGB(1, 1, 1);
        cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Normal);
        cr.SetFontSize(14);
        
        string text = $"({(int) wx}, {(int) wy})";
        
        cr.MoveTo(width - 10 - cr.TextExtents(text).Width, height - 10);
        cr.ShowText(text);
    }

    public int selected = -1;
    public string selectedComponent = "";
    
    public void DrawComponent(Context cr, Component component, Pin? draggedPin, int draggedState)
    {
        var (x, y, x1, y1) = GetComponentBounds(component);

        var (sx, sy) = WorldToScreen(x, y);
        var (sx1, sy1) = WorldToScreen(x1, y1);

        cr.LineWidth = 7 * camera.zoom;

        double size = Math.Abs(y1 - y) * camera.zoom;
        double pinOffset = 20 * camera.zoom;

        cr.LineCap = LineCap.Round;

        if (component.Inputs.Count > 0)
        {
            double inputSpacing = (size + pinOffset) / (component.Inputs.Count + 1);

            for (int i = 0; i < component.Inputs.Count; i++)
            {
                if (component.Inputs[i].State)
                {
                    cr.SetSourceRGB(0.95, 0.18, 0.18);
                }
                else
                {
                    cr.SetSourceRGB(0.43, 0.43, 0.43);
                }

                cr.MoveTo(sx, sy + inputSpacing * (i + 1) - (pinOffset / 2));
                cr.LineTo(sx - 15 * camera.zoom, sy + inputSpacing * (i + 1) - (pinOffset / 2));
                cr.Stroke();

                if (component.Inputs[i].Component != null)
                {
                    var (cx, cy, cx1, cy1) = GetComponentBounds(component.Inputs[i].Component);

                    var (csx, csy) = WorldToScreen(cx, cy);
                    var (csx1, csy1) = WorldToScreen(cx1, cy1);

                    double oSize = Math.Abs(cy1 - cy) * camera.zoom;
                    double outputSpacing = (oSize + pinOffset) / (component.Inputs[i].Component.Outputs.Count + 1);

                    for (int j = 0; j < component.Inputs[i].Component.Outputs.Count; j++)
                    {
                        if (component.Inputs[i].Component.Outputs[j] == component.Inputs[i])
                        {
                            double controlPointX = (csx1 + sx) / 2;
                            double controlPointY1 = csy + outputSpacing * (j + 1) - (pinOffset / 2);
                            double controlPointY2 = sy + inputSpacing * (i + 1) - (pinOffset / 2);

                            cr.MoveTo(csx1 + 15 * camera.zoom, controlPointY1);
                            cr.CurveTo(controlPointX, controlPointY1, controlPointX, controlPointY2, sx - 15 * camera.zoom, controlPointY2);
                            cr.Stroke();
                        }
                    }
                }
                
                if (draggedPin == component.Inputs[i] && draggedState == 1)
                {
                    var (mx, my) = (ProgramWindow.currentX, ProgramWindow.currentY);
                    
                    cr.MoveTo(sx - 15 * camera.zoom, sy + inputSpacing * (i + 1) - (pinOffset / 2));
                    cr.LineTo(mx, my);
                    cr.Stroke();
                }
            }
        }

        if (component.Outputs.Count > 0)
        {
            double outputSpacing = (size + pinOffset) / (component.Outputs.Count + 1);

            for (int i = 0; i < component.Outputs.Count; i++)
            {
                if (component.Outputs[i].State)
                {
                    cr.SetSourceRGB(0.95, 0.18, 0.18);
                }
                else
                {
                    cr.SetSourceRGB(0.43, 0.43, 0.43);
                }

                cr.MoveTo(sx1, sy + outputSpacing * (i + 1) - (pinOffset / 2));
                cr.LineTo(sx1 + 15 * camera.zoom, sy + outputSpacing * (i + 1) - (pinOffset / 2));
                cr.Stroke();
                
                if (draggedPin == component.Outputs[i] && draggedState == 2)
                {
                    var (mx, my) = (ProgramWindow.currentX, ProgramWindow.currentY);

                    cr.MoveTo(sx1 + 15 * camera.zoom, sy + outputSpacing * (i + 1) - (pinOffset / 2));
                    cr.LineTo(mx, my);
                    cr.Stroke();
                }
            }
        }
        
        var (color1, color2) = component.GetColors();

        cr.SetSourceRGB(color1.r, color1.g, color1.b);

        cr.Rectangle(sx, sy, sx1 - sx, sy1 - sy);

        cr.Fill();

        if (!(component is LCDDisplay))
        {
            cr.SetSourceRGB(color2.r, color2.g, color2.b);
            cr.LineWidth = 2.5 * camera.zoom;
            double offset = 4 * camera.zoom;
            cr.Rectangle(sx + offset, sy + offset, sx1 - sx - offset * 2, sy1 - sy - offset * 2);

            cr.Stroke();
        }

        cr.SetSourceRGB(1, 1, 1);
        cr.LineWidth = 0.2 * camera.zoom;
        cr.Rectangle(sx, sy, sx1 - sx, sy1 - sy);
        
        cr.Stroke();

        if (!(component is Light || component is Button || component is SevenSegmentDisplay))
        {
            cr.SetSourceRGB(1, 1, 1);
            cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Bold);

            cr.SetFontSize(19 * camera.zoom);
            string[] lines = component.Name.Split('\n');

            double totalHeight = lines.Length * (cr.FontExtents.Ascent + cr.FontExtents.Descent);
            double yy = sy + (sy1 - sy) / 2 - totalHeight / 2 + cr.FontExtents.Ascent;

            foreach (string line in lines)
            {
                TextExtents te = cr.TextExtents(line);
                double xx = sx + (sx1 - sx) / 2 - te.Width / 2;
                cr.MoveTo(xx, yy);
                cr.ShowText(line);
                yy += cr.FontExtents.Height;
            }
        }

        if (component is Light)
        {
            if (component.Inputs.Count > 0 && component.Inputs[0].State)
            {
                cr.SetSourceRGB(0.95, 0.18, 0.18);
            }
            else
            {
                cr.SetSourceRGB(0.05, 0.05, 0.05);
            }

            cr.Rectangle(sx + 10 * camera.zoom, sy + 10 * camera.zoom, (sx1 - sx - 20) * camera.zoom, (sy1 - sy - 20) * camera.zoom);
            cr.Fill();
        }

        if (component is Button)
        {
            if (component.Outputs.Count > 0 && component.Outputs[0].State)
            {
                cr.SetSourceRGB(0.95, 0.18, 0.18);
            }
            else
            {
                cr.SetSourceRGB(0.05, 0.05, 0.05);
            }

            cr.Rectangle(sx + 10 * camera.zoom, sy + 10 * camera.zoom, (sx1 - sx - 20) * camera.zoom, (sy1 - sy - 20) * camera.zoom);
            cr.Fill();
        }
        
        if (component is SevenSegmentDisplay)
        {
            cr.LineWidth = 11 * camera.zoom;
            cr.LineCap = LineCap.Round;
            
            double lineOffset = 20 * camera.zoom;
            double lineLength = 22 * camera.zoom;
            
            double x2 = sx + (sx1 - sx) / 2;
            double y2 = sy + (sy1 - sy) / 2;
            
            // Top (a)
            if (component.Inputs[0].State)
            {
                cr.SetSourceRGB(0.95, 0.18, 0.18);
            }
            else
            {
                cr.SetSourceRGB(0.05, 0.05, 0.05);
            }
            
            cr.MoveTo(x2 - lineLength, sy + lineOffset);
            cr.LineTo(x2 + lineLength, sy + lineOffset);
            cr.Stroke();
            
            // Top right (b)
            if (component.Inputs[1].State)
            {
                cr.SetSourceRGB(0.95, 0.18, 0.18);
            }
            else
            {
                cr.SetSourceRGB(0.05, 0.05, 0.05);
            }
            
            cr.MoveTo(sx1 - lineOffset, y2 - lineLength * 2.15);
            cr.LineTo(sx1 - lineOffset, y2 - lineLength / 3);
            cr.Stroke();
            
            // Bottom right (c)
            if (component.Inputs[2].State)
            {
                cr.SetSourceRGB(0.95, 0.18, 0.18);
            }
            else
            {
                cr.SetSourceRGB(0.05, 0.05, 0.05);
            }
            
            cr.MoveTo(sx1 - lineOffset, y2 + lineLength / 3);
            cr.LineTo(sx1 - lineOffset, y2 + lineLength * 2.15);
            cr.Stroke();
            
            // Bottom (d)
            if (component.Inputs[3].State)
            {
                cr.SetSourceRGB(0.95, 0.18, 0.18);
            }
            else
            {
                cr.SetSourceRGB(0.05, 0.05, 0.05);
            }
            
            cr.MoveTo(x2 - lineLength, sy1 - lineOffset);
            cr.LineTo(x2 + lineLength, sy1 - lineOffset);
            cr.Stroke();
            
            // Bottom left (e)
            if (component.Inputs[4].State)
            {
                cr.SetSourceRGB(0.95, 0.18, 0.18);
            }
            else
            {
                cr.SetSourceRGB(0.05, 0.05, 0.05);
            }
            
            cr.MoveTo(sx + lineOffset, y2 + lineLength * 2.15);
            cr.LineTo(sx + lineOffset, y2 + lineLength / 3);
            cr.Stroke();
            
            // Top left (f)
            if (component.Inputs[5].State)
            {
                cr.SetSourceRGB(0.95, 0.18, 0.18);
            }
            else
            {
                cr.SetSourceRGB(0.05, 0.05, 0.05);
            }
            
            cr.MoveTo(sx + lineOffset, y2 - lineLength / 3);
            cr.LineTo(sx + lineOffset, y2 - lineLength * 2.15);
            cr.Stroke();
            
            // Middle (g)
            if (component.Inputs[6].State)
            {
                cr.SetSourceRGB(0.95, 0.18, 0.18);
            }
            else
            {
                cr.SetSourceRGB(0.05, 0.05, 0.05);
            }
            
            cr.MoveTo(x2 - lineLength / 1.2, y2);
            cr.LineTo(x2 + lineLength / 1.2, y2);
            cr.Stroke();
        }

        if (component is LCDDisplay lcd)
        {
            cr.SetSourceRGB(color2.r, color2.g, color2.b);
            double offset = 5 * camera.zoom;
            cr.Rectangle(sx + offset, sy + offset, sx1 - sx - offset * 2, sy1 - sy - offset * 2);

            cr.Fill();
            
            offset = 12 * camera.zoom;
            cr.SetSourceRGB(0.07, 0.07, 0.07);
            cr.Rectangle(sx + offset, sy + offset, sx1 - sx - offset * 2, sy1 - sy - offset * 2);
            cr.Fill();
            
            double circleSize = 4 * camera.zoom;
            offset = 7 * camera.zoom;
            // EBBF1E
            cr.SetSourceRGB(0.92, 0.75, 0.12);
            cr.Arc(sx + offset, sy + offset, circleSize, 0, 2 * Math.PI);
            cr.Fill();
            cr.SetSourceRGB(0.07, 0.07, 0.07);
            cr.Arc(sx + offset, sy + offset, circleSize / 2, 0, 2 * Math.PI);
            cr.Fill();
            
            cr.SetSourceRGB(0.92, 0.75, 0.12);
            cr.Arc(sx1 - offset, sy + offset, circleSize, 0, 2 * Math.PI);
            cr.Fill();
            cr.SetSourceRGB(0.07, 0.07, 0.07);
            cr.Arc(sx1 - offset, sy + offset, circleSize / 2, 0, 2 * Math.PI);
            cr.Fill();
            
            cr.SetSourceRGB(0.92, 0.75, 0.12);
            cr.Arc(sx + offset, sy1 - offset, circleSize, 0, 2 * Math.PI);
            cr.Fill();
            cr.SetSourceRGB(0.07, 0.07, 0.07);
            cr.Arc(sx + offset, sy1 - offset, circleSize / 2, 0, 2 * Math.PI);
            cr.Fill();
            
            cr.SetSourceRGB(0.92, 0.75, 0.12);
            cr.Arc(sx1 - offset, sy1 - offset, circleSize, 0, 2 * Math.PI);
            cr.Fill();
            cr.SetSourceRGB(0.07, 0.07, 0.07);
            cr.Arc(sx1 - offset, sy1 - offset, circleSize / 2, 0, 2 * Math.PI);
            cr.Fill();
            
            // #78bc2c
            offset = 25 * camera.zoom;
            
            cr.SetSourceRGB(0.47, 0.74, 0.17);
            cr.Rectangle(sx + offset, sy + offset, sx1 - sx - offset * 2, sy1 - sy - offset * 2);
            cr.Fill();
            
            // ##70ac24
            offset = 30 * camera.zoom;
            double fieldHeight = 18 * camera.zoom;
            double fieldWidth = 12 * camera.zoom;
            
            double fieldPadding = 3 * camera.zoom;

            char[] data = lcd.getData();
            
            for (int i = 0; i < lcd.getRows(); i++)
            {
                for (int j = 0; j < lcd.getCols(); j++)
                {
                    cr.SetSourceRGB(0.44, 0.67, 0.14);
                    
                    cr.Rectangle(sx + offset + j * (fieldWidth + fieldPadding), sy + offset + i * (fieldHeight + fieldPadding), fieldWidth, fieldHeight);
                    cr.Fill();
                    
                    cr.SetSourceRGB(0.07, 0.07, 0.07);
                    
                    cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Bold);
                    cr.SetFontSize(12 * camera.zoom);
                    
                    double textXOffset = 0 * camera.zoom;
                    double textYOffset = 4.5 * camera.zoom;
                  
                     char c = data[i * lcd.getCols() + j];
                     string str = c.ToString();
                    
                     TextExtents te = cr.TextExtents(str);
                    
                     double textWidth = te.Width;
                    
                     cr.MoveTo(
                         sx + offset + j * (fieldWidth + fieldPadding) + fieldWidth / 2 - textWidth / 2 +
                         textXOffset,
                         sy + offset + i * (fieldHeight + fieldPadding) + fieldHeight / 2 + textYOffset);
                     cr.ShowText(str);
                
                     if (c == '\0')
                     {
                         cr.SetSourceRGB(0.33, 0.51, 0.11);
                         cr.Rectangle(sx + offset + j * (fieldWidth + fieldPadding),
                             sy + offset + i * (fieldHeight + fieldPadding), fieldWidth, fieldHeight);
                         cr.Fill();
                     }
                }
            }
        }
        
        if (true) return;

        if (component is KeyboardModule keyboard)
        {
            cr.SetSourceRGB(0.05, 0.05, 0.05);
            double offset = 12 * camera.zoom;
            cr.Rectangle(sx + offset, sy + offset, sx1 - sx - offset * 2, sy1 - sy - offset * 2);
            cr.Fill();
            
            cr.SetSourceRGB(1, 1, 1);
            cr.SelectFontFace("Arial", FontSlant.Normal, FontWeight.Bold);
            cr.SetFontSize(50 * camera.zoom);
            
            TextExtents te = cr.TextExtents(keyboard.lastChar.ToString());
            
            double textWidth = te.Width;
            double textHeight = te.Height;
            
            double yoffset = 0 * camera.zoom;
            
            if (keyboard.lastChar == 'g' || keyboard.lastChar == 'j' || keyboard.lastChar == 'p' || keyboard.lastChar == 'q' || keyboard.lastChar == 'Q' || keyboard.lastChar == 'y' || keyboard.lastChar == '@')
            {
                yoffset = 7.5 * camera.zoom;
            }
            
            cr.MoveTo(sx + (sx1 - sx) / 2 - textWidth / 2, sy + (sy1 - sy) / 2 + textHeight / 2 - yoffset);
            cr.ShowText(keyboard.lastChar.ToString());
        }
    }
    
    public (double, double, double, double) GetComponentBounds(Component component)
    {
        if (component is Light || component is Button)
        {
            double size = 40 * camera.zoom;
            return (component.x, component.y, component.x + size, component.y + size);
        }
        else if (component is LCDDisplay lcd)
        {
            double lheight = 57 * camera.zoom + (18 + 3) * camera.zoom * lcd.getRows();
            double lwidth = 57 * camera.zoom + (12 + 3) * camera.zoom * lcd.getCols();
            
            return (component.x, component.y, component.x + lwidth, component.y + lheight);
        }
        else if (component is KeyboardModule)
        {
            double size = 100 * camera.zoom;
            
            return (component.x, component.y, component.x + size, component.y + size);
        }

        double maxHeight = 20 + Math.Max(component.Inputs.Count, component.Outputs.Count) * 20;
        
        if (maxHeight < 40)
        {
            maxHeight = 40;
        }
    
        double width = 100 * camera.zoom;
        double height = maxHeight * camera.zoom;
    
        return (component.x, component.y, component.x + width, component.y + height);
    }

    
    public (double, double) ScreenToWorld(double x, double y)
    {
        return (x / camera.zoom - camera.x, y / camera.zoom - camera.y);
    }
    
    public (double, double) WorldToScreen(double x, double y)
    {
        return ((x + camera.x) * camera.zoom, (y + camera.y) * camera.zoom);
    }

    public Component? GetClickedComponent(double x, double y)
    {
        var (wx, wy) = ScreenToWorld(x, y);
        
        foreach (var component in components.Reverse<Component>())
        {
            var (x1, y1, x2, y2) = GetComponentBounds(component);

            x1 -= 20;
            x2 += 20;
            
            if (wx >= x1 && wx <= x2 && wy >= y1 && wy <= y2)
            {
                return component;
            }
        }
        
        return null;
    }
    
    public void OnScroll(double delta)
    {
        camera.zoom += delta / 10.0;
    }

    public class SerializableSchematic
    {
        public Camera Camera { get; set; }
        
        public List<(double, double, CompoundComponent.SerializedComponent)> Components { get; set; }
        
        public SerializableSchematic(Camera camera, List<(double, double, CompoundComponent.SerializedComponent)> components)
        {
            Camera = camera;
            Components = components;
        }
    }
    
    public string Serialize()
    {
        List<(double, double, CompoundComponent.SerializedComponent)> serializedComponents = new();
        
        List<Pin> pins = new();
        
        foreach (var component in components)
        {
            foreach (var pin in component.Inputs)
            {
                pins.Add(pin);
            }
            
            foreach (var pin in component.Outputs)
            {
                pins.Add(pin);
            }
        }
        
        // Remove duplicates
        pins = pins.Distinct().ToList();
        
        foreach (var component in components)
        {
            List<int> inputs = new();
            List<int> outputs = new();
            
            foreach (Pin pin in component.Inputs)
            {
                inputs.Add(pins.IndexOf(pin));
            }
            
            foreach (Pin pin in component.Outputs)
            {
                outputs.Add(pins.IndexOf(pin));
            }
            
            serializedComponents.Add((component.x, component.y, new CompoundComponent.SerializedComponent(component.Name, inputs, outputs)));
        }
        
        return JsonConvert.SerializeObject(new SerializableSchematic(camera, serializedComponents));
    }
    
    public static Scene Deserialize(string serialized)
    {
        SerializableSchematic schematic = JsonConvert.DeserializeObject<SerializableSchematic>(serialized);
        
        Scene scene = new();
        
        int pinCount = 0;
        List<Pin> pins = new();
        
        foreach (var serializedComponent in schematic.Components)
        {
            foreach (int pin in serializedComponent.Item3.Inputs)
            {
                if (pin >= pinCount)
                {
                    pinCount = pin;
                }
            }
            
            foreach (int pin in serializedComponent.Item3.Outputs)
            {
                if (pin >= pinCount)
                {
                    pinCount = pin;
                }
            }
        }
        
        for (int i = 0; i <= pinCount; i++)
        {
            pins.Add(new Pin());
        }
        
        foreach (var serializedComponent in schematic.Components)
        {
            Component component = Component.GetByName(serializedComponent.Item3.Name);
            
            component.x = serializedComponent.Item1;
            component.y = serializedComponent.Item2;
            
            for (int i = 0; i < serializedComponent.Item3.Inputs.Count; i++)
            {
                component.Inputs[i] = pins[serializedComponent.Item3.Inputs[i]];
            }
            
            for (int i = 0; i < serializedComponent.Item3.Outputs.Count; i++)
            {
                component.Outputs[i] = pins[serializedComponent.Item3.Outputs[i]];
                pins[serializedComponent.Item3.Outputs[i]].Component = component;
            }
            
            scene.AddComponent(component);
        }
        
        scene.camera = schematic.Camera;
        
        return scene;
    }
}