namespace Atestat.Scene;

public class Counter8 : Component
{
    byte value = 0;
    
    public Counter8()
    {
        for (int i = 0; i < 2; i++)
        {
            Inputs.Add(new Pin());
        }
        
        for (int i = 0; i < 8; i++)
        {
            Outputs.Add(new Pin(this));
        }
        
        Name = "8-BIT\nCTR";
    }
    
    public override void Update()
    {
        if (Inputs[1].State)
        {
            value = 0;
        }
        else if (Inputs[0].State)
        {
            if (value == 255)
            {
                value = 0;
            }
            else
            {
                value++;
            }
        }
        
        for (int i = 0; i < 8; i++)
        {
            Outputs[i].State = (value & (1 << (7 - i))) != 0;
        }
    }
}

public class Counter4 : Component
{
    byte value = 0;
    
    public Counter4()
    {
        for (int i = 0; i < 2; i++)
        {
            Inputs.Add(new Pin());
        }
        
        for (int i = 0; i < 4; i++)
        {
            Outputs.Add(new Pin(this));
        }
        
        Name = "4-BIT\nCTR";
    }
    
    public override void Update()
    {
        if (Inputs[1].State)
        {
            value = 0;
        }
        else if (Inputs[0].State)
        {
            if (value == 15)
            {
                value = 0;
            }
            else
            {
                value++;
            }
        }
        
        for (int i = 0; i < 4; i++)
        {
            Outputs[i].State = (value & (1 << (3 - i))) != 0;
        }
    }
}

// Modulator. Takes an input and outputs it for a single clock cycle, and then locks itself until the input changes.
public class Modulator : Component
{
    private bool state = true;
    
    public Modulator()
    {
        Inputs.Add(new Pin());
        Outputs.Add(new Pin(this));
        
        Name = "MOD";
    }
    
    public override void Update()
    {
        if (state)
        {
            Outputs[0].State = Inputs[0].State;
            state = false;
        }
        else
        {
            Outputs[0].State = false;
        }
        
        if (!Inputs[0].State)
        {
            state = true;
        }
    }
}

// LCD Display.
public class LCDDisplay : Component
{
    public int width = 16;
    public int height = 2;
    
    // its a 16x2 display
    public char[] data = new char[32];
    public LCDDisplay()
    {
        for (int i = 0; i < 8; i++)
        {
            Inputs.Add(new Pin());
        }
        
        Name = "LCD";
        
        for (int i = 0; i < 32; i++)
        {
            data[i] = '\0';
        }
    }
    
    int cursor = 0;
    public override void Update()
    {
        if (Inputs[0].State)
        {
            data[cursor] = (char)Inputs[1..8].Select(x1 => x1.State ? 1 : 0).Aggregate((x, y) => x * 2 + y);
            
            cursor++;
            if (cursor == getData().Length)
            {
                cursor = 0;
            }
        }
    }
    
    public override (Color, Color) GetColors()
    {
        // #088434 & #07692D
        return (new Color(0.03, 0.51, 0.2), new Color(0.03, 0.41, 0.18));
    }
    
    public virtual int getRows()
    {
        return height;
    }
    
    public virtual int getCols()
    {
        return width;
    }
    
    public virtual char[] getData()
    {   
        return data;
    }
}

// LCD Display Big (16x16)
public class LCDDisplayBig : LCDDisplay
{
    public new int width = 16;
    public new int height = 16;
    
    public override int getRows()
    {
        return height;
    }
    
    public override int getCols()
    {
        return width;
    }
    
    public LCDDisplayBig()
    {
        Name = "LCD\nBIG";
        
        data = new char[256];
        
        for (int i = 0; i < 256; i++)
        {
            data[i] = '\0';
        }
    }
    
    public override char[] getData()
    {
        return data;
    }
    
    public override (Color, Color) GetColors()
    {
        // #088434 & #07692D
        return (new Color(0.03, 0.51, 0.2), new Color(0.03, 0.41, 0.18));
    }
}

// Keyboard Module
public class KeyboardModule : Component
{
    public char lastChar = '\0';
    
    public KeyboardModule()
    {
        for (int i = 0; i < 8; i++)
        {
            Outputs.Add(new Pin(this));
        }
        
        Name = "KBD";
    }

    public override void Update()
    {
        Outputs[0].State = false;
        if (ProgramWindow.chars.Count > 0)
        {
            lastChar = ProgramWindow.chars[0];
            Outputs[0].State = true;
        }
        
        for (int i = 0; i < 7; i++)
        {
            Outputs[i + 1].State = (lastChar & (1 << (6 - i))) != 0;
        }
    }
}