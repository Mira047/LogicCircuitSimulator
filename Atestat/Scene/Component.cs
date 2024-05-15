namespace Atestat.Scene;    

using System;
using Newtonsoft.Json;

public class Component
{
    public double x, y;
    
    public string Name { get; set; }
    
    public List<Pin> Inputs { get; set; }
    public List<Pin> Outputs { get; set; }
    
    public Component()
    {
        Inputs = [];
        Outputs = [];
    }
    
    public virtual void Update()
    {
        throw new NotImplementedException();
    }
    
    public virtual (Color, Color) GetColors()
    {
        return (new Color(0.1, 0.1, 0.1), new Color(0.15, 0.15, 0.15));
    }

    public static Component GetByName(string name)
    {
        switch (name)
        {
            case "8-BIT\nCTR":
                return new Counter8();
            case "4-BIT\nCTR":
                return new Counter4();
            case "AND":
                return new AndGate();
            case "OR":
                return new OrGate();
            case "NOT":
                return new NotGate();
            case "XOR":
                return new XorGate();
            case "NAND":
                return new NandGate();
            case "NOR":
                return new NorGate();
            case "XNOR":
                return new XnorGate();
            case "CLOCK":
                return new Clock();
            case "BUTTON":
                return new Button();
            case "LIGHT":
                return new Light();
            case "7SEG":
                return new SevenSegmentDisplay();
            case "7SEG/nDRIVER":
                return new SevenSegmentDriver();
            case "LCD":
                return new LCDDisplay();
            case "LCD\nBIG":
                return new LCDDisplayBig();
            case "MOD":
                return new Modulator();
            case "KBD":
                return new KeyboardModule();
            case "SNAKE\nCPU":
                return new Snake();
            default:
                return Scene.LoadCustomComponent("custom_components/" + name + ".lcc");
        }
    }
}

public class Pin
{
    public bool State { get; set; }
    public Component? Component { get; set; }
    
    public Pin(Component component)
    {
        State = false;
        Component = component;
    }
    
    public Pin()
    {
        State = false;
        Component = null;
    }
}

public class CompoundComponent : Component
{
    List<Component> internalComponents;
    List<Button> inputList = new();
    List<Light> outputList = new();
    
    int inputs, outputs;
    
    public CompoundComponent(List<Component> components, String name)
    {
        internalComponents = components;
        
        inputs = 0;
        outputs = 0;
        
        this.Name = name;
        
        foreach (Component component in components)
        {
            if (component is Button button)
            {
                inputs++;
                inputList.Add(button);
            }
            else if (component is Light light)
            {
                outputs++;
                outputList.Add(light);
            }
        }
        
        for (int i = 0; i < inputs; i++)
        {
            Inputs.Add(new Pin());
        }
        
        for (int i = 0; i < outputs; i++)
        {
            Outputs.Add(new Pin(this));
        }
    }
    
    public override void Update()
    {
        for (int i = 0; i < inputs; i++)
        {
            inputList[i].Set(Inputs[i].State);
        }
        
        foreach (Component component in internalComponents)
        {
            component.Update();
        }
        
        for (int i = 0; i < outputs; i++)
        {
            Outputs[i].State = outputList[i].Get();
        }
    }
    
    public string serialize()
    {
        string result = "";
        
        List<Pin> pins = new();
        List<SerializedComponent> serializedComponents = new();
        
        foreach (Component component in internalComponents)
        {
            foreach (Pin pin in component.Inputs)
            {
                if (!pins.Contains(pin))
                {
                    pins.Add(pin);
                }
            }
            
            foreach (Pin pin in component.Outputs)
            {
                if (!pins.Contains(pin))
                {
                    pins.Add(pin);
                }
            }
        }
        
        foreach (Component component in internalComponents)
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
            
            serializedComponents.Add(new SerializedComponent(component.Name, inputs, outputs));
        }
        
        string[] splitName = Name.Split(' ');
        
        int amountOfChars = 0;
        for (int i = 0; i < splitName.Length; i++)
        {
            amountOfChars += splitName[i].Length + 1;
            
            if (amountOfChars > 9)
            {
                splitName[i] = "\n" + splitName[i];
                amountOfChars = 0;
            }
            else if (i != splitName.Length - 1)
            {
                splitName[i] += " ";
            }
        }
        
        string name = "";
        foreach (string s in splitName)
        {
            name += s;
        }
        
        SerializedCompoundComponent serializedCompoundComponent = new SerializedCompoundComponent(name, serializedComponents);
        
        result = JsonConvert.SerializeObject(serializedCompoundComponent);
        
        return result;
    }
    
    public static CompoundComponent deserialize(string serialized)
    {
        SerializedCompoundComponent serializedCompoundComponent = JsonConvert.DeserializeObject<SerializedCompoundComponent>(serialized);
        
        List<Component> components = new();
        
        List<Pin> pins = new();
        
        int maxPin = 0;
        foreach (SerializedComponent serializedComponent in serializedCompoundComponent.Components)
        {
            foreach (int pin in serializedComponent.Inputs)
            {
                if (pin > maxPin)
                {
                    maxPin = pin;
                }
            }
        }
        
        for (int i = 0; i <= maxPin; i++)
        {
            pins.Add(new Pin());
        }
        
        foreach (SerializedComponent serializedComponent in serializedCompoundComponent.Components)
        {
            
            Component component = Component.GetByName(serializedComponent.Name);
            
            for (int i = 0; i < serializedComponent.Inputs.Count; i++)
            {
                component.Inputs[i] = pins[serializedComponent.Inputs[i]];
            }
            
            for (int i = 0; i < serializedComponent.Outputs.Count; i++)
            {
                component.Outputs[i] = pins[serializedComponent.Outputs[i]];
            }
            
            components.Add(component);
        }
        
        return new CompoundComponent(components, serializedCompoundComponent.Name);
    }

    public class SerializedComponent
    {
        public string Name { get; set; }
        public List<int> Inputs { get; set; }
        public List<int> Outputs { get; set; }
        
        public SerializedComponent(string name, List<int> inputs, List<int> outputs)
        {
            Name = name;
            Inputs = inputs;
            Outputs = outputs;
        }
    }
    
    public class SerializedCompoundComponent
    {
        public string Name { get; set; }
        public List<SerializedComponent> Components { get; set; }
        
        public SerializedCompoundComponent(string name, List<SerializedComponent> components)
        {
            Name = name;
            Components = components;
        }
    }
}