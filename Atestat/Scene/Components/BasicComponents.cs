namespace Atestat.Scene;

public class OrGate : Component
{
    public OrGate()
    {
        Inputs.Add(new Pin());
        Inputs.Add(new Pin());
        Outputs.Add(new Pin(this));
        
        Name = "OR";
    }
    
    public override void Update()
    {
        Outputs[0].State = Inputs[0].State || Inputs[1].State;
    }
}

public class AndGate : Component
{
    public AndGate()
    {
        Inputs.Add(new Pin());
        Inputs.Add(new Pin());
        Outputs.Add(new Pin(this));
        
        Name = "AND";
    }
    
    public override void Update()
    {
        Outputs[0].State = Inputs[0].State && Inputs[1].State;
    }
}

public class NotGate : Component
{
    public NotGate()
    {
        Inputs.Add(new Pin());
        Outputs.Add(new Pin(this));
        
        Name = "NOT";
    }
    
    public override void Update()
    {
        Outputs[0].State = !Inputs[0].State;
    }
}

public class XorGate : Component
{
    public XorGate()
    {
        Inputs.Add(new Pin());
        Inputs.Add(new Pin());
        Outputs.Add(new Pin(this));
        
        Name = "XOR";
    }
    
    public override void Update()
    {
        Outputs[0].State = Inputs[0].State ^ Inputs[1].State;
    }
}

public class NandGate : Component
{
    public NandGate()
    {
        Inputs.Add(new Pin());
        Inputs.Add(new Pin());
        Outputs.Add(new Pin(this));
        
        Name = "NAND";
    }
    
    public override void Update()
    {
        Outputs[0].State = !(Inputs[0].State && Inputs[1].State);
    }
}

public class NorGate : Component
{
    public NorGate()
    {
        Inputs.Add(new Pin());
        Inputs.Add(new Pin());
        Outputs.Add(new Pin(this));
        
        Name = "NOR";
    }
    
    public override void Update()
    {
        Outputs[0].State = !(Inputs[0].State || Inputs[1].State);
    }
}

public class XnorGate : Component
{
    public XnorGate()
    {
        Inputs.Add(new Pin());
        Inputs.Add(new Pin());
        Outputs.Add(new Pin(this));
        
        Name = "XNOR";
    }
    
    public override void Update()
    {
        Outputs[0].State = !(Inputs[0].State ^ Inputs[1].State);
    }
}


public class Clock : Component
{
    private static List<Clock> clocks = new List<Clock>();
    
    private int counter, period, duration;
    public Clock()
    {
        period = (int) (Program.FRAME_RATE / 1);
        counter = 0;
        Outputs.Add(new Pin(this));

        this.duration = period;
        
        Name = "CLOCK";
        
        foreach (Clock clock in clocks)
        {
            clock.counter = 0;
            clock.Outputs[0].State = false;
        }
        clocks.Add(this);
    }
    
    ~Clock()
    {
        clocks.Remove(this);
    }
    
    public override void Update()
    {
        counter++;
        if (!Outputs[0].State)
        {
            if (counter == period)
            {
                Outputs[0].State = true;
                counter = 0;
            }
        }
        else
        {
            if (counter == duration)
            {
                Outputs[0].State = false;
                counter = 0;
            }
        }
    }
}

public class Button : Component
{
    public Button()
    {
        Outputs.Add(new Pin(this));
        
        Name = "BUTTON";
    }
    
    public void Press()
    {
        Outputs[0].State = !Outputs[0].State;
    }
    
    public void Set(bool state)
    {
        Outputs[0].State = state;
    }
    
    public override void Update()
    {
        // Do nothing
    }
}

public class Light : Component
{
    public Light()
    {
        Inputs.Add(new Pin());
        
        Name = "LIGHT";
    }
    
    public bool Get()
    {
        return Inputs[0].State;
    }
    
    public override void Update()
    {
        // Do nothing
    }
}

public class SevenSegmentDisplay : Component
{
    public SevenSegmentDisplay()
    {
        for (int i = 0; i < 7; i++)
        {
            Inputs.Add(new Pin());
            
            Name = "7SEG";
        }
    }

    public override void Update()
    {
        // Do nothing
    }
}

public class SevenSegmentDriver : Component
{
    public SevenSegmentDriver()
    {
        for (int i = 0; i < 4; i++)
        {
            Inputs.Add(new Pin());
        }
        
        for (int i = 0; i < 7; i++)
        {
            Outputs.Add(new Pin(this));
        }
        
        Name = "7SEG\nDRIVER";
    }
    
    public override void Update()
    {
        bool[] segments = new bool[7];
        
        // 0
        if (!Inputs[0].State && !Inputs[1].State && !Inputs[2].State && !Inputs[3].State)
        {
            segments[0] = true;
            segments[1] = true;
            segments[2] = true;
            segments[3] = true;
            segments[4] = true;
            segments[5] = true;
        }
        // 1
        else if (!Inputs[0].State && !Inputs[1].State && !Inputs[2].State && Inputs[3].State)
        {
            segments[1] = true;
            segments[2] = true;
        }
        // 2
        else if (!Inputs[0].State && !Inputs[1].State && Inputs[2].State && !Inputs[3].State)
        {
            segments[0] = true;
            segments[1] = true;
            segments[6] = true;
            segments[4] = true;
            segments[3] = true;
        }
        
        // 3
        else if (!Inputs[0].State && !Inputs[1].State && Inputs[2].State && Inputs[3].State)
        {
            segments[0] = true;
            segments[1] = true;
            segments[2] = true;
            segments[3] = true;
            segments[6] = true;
        }
        
        // 4
        else if (!Inputs[0].State && Inputs[1].State && !Inputs[2].State && !Inputs[3].State)
        {
            segments[5] = true;
            segments[2] = true;
            segments[1] = true;
            segments[6] = true;
        }
        
        // 5
        else if (!Inputs[0].State && Inputs[1].State && !Inputs[2].State && Inputs[3].State)
        {
            segments[0] = true;
            segments[5] = true;
            segments[2] = true;
            segments[3] = true;
            segments[6] = true;
        }
        
        // 6
        else if (!Inputs[0].State && Inputs[1].State && Inputs[2].State && !Inputs[3].State)
        {
            segments[0] = true;
            segments[5] = true;
            segments[2] = true;
            segments[3] = true;
            segments[4] = true;
            segments[6] = true;
        }
        
        // 7
        else if (!Inputs[0].State && Inputs[1].State && Inputs[2].State && Inputs[3].State)
        {
            segments[0] = true;
            segments[1] = true;
            segments[2] = true;
        }
        
        // 8
        else if (Inputs[0].State && !Inputs[1].State && !Inputs[2].State && !Inputs[3].State)
        {
            segments[0] = true;
            segments[1] = true;
            segments[2] = true;
            segments[3] = true;
            segments[4] = true;
            segments[5] = true;
            segments[6] = true;
        }
        
        // 9
        else if (Inputs[0].State && !Inputs[1].State && !Inputs[2].State && Inputs[3].State)
        {
            segments[0] = true;
            segments[1] = true;
            segments[2] = true;
            segments[3] = true;
            segments[5] = true;
            segments[6] = true;
        }
        
        // A
        else if (Inputs[0].State && !Inputs[1].State && Inputs[2].State && !Inputs[3].State)
        {
            segments[0] = true;
            segments[1] = true;
            segments[2] = true;
            segments[4] = true;
            segments[5] = true;
            segments[6] = true;
        }
        
        // B
        else if (Inputs[0].State && !Inputs[1].State && Inputs[2].State && Inputs[3].State)
        {
            segments[0] = true;
            segments[1] = true;
            segments[2] = true;
            segments[3] = true;
            segments[4] = true;
            segments[5] = true;
            segments[6] = true;
        }
        
        // C
        else if (Inputs[0].State && Inputs[1].State && !Inputs[2].State && !Inputs[3].State)
        {
            segments[0] = true;
            segments[3] = true;
            segments[4] = true;
            segments[5] = true;
        }
        
        // D
        else if (Inputs[0].State && Inputs[1].State && !Inputs[2].State && Inputs[3].State)
        {
            // (same as 0)
            segments[0] = true;
            segments[1] = true;
            segments[2] = true;
            segments[3] = true;
            segments[4] = true;
            segments[5] = true;
        }
        
        // E
        else if (Inputs[0].State && Inputs[1].State && Inputs[2].State && !Inputs[3].State)
        {
            segments[0] = true;
            segments[3] = true;
            segments[4] = true;
            segments[5] = true;
            segments[6] = true;
        }
        
        // F
        else if (Inputs[0].State && Inputs[1].State && Inputs[2].State && Inputs[3].State)
        {
            segments[0] = true;
            segments[4] = true;
            segments[5] = true;
            segments[6] = true;
        }

        for (int i = 0; i < 7; i++)
        {
            Outputs[i].State = segments[i];
        }
    }
}