namespace Atestat.Scene;

public class Snake : Component
{
    private int counter = 256;
    
    private Queue<(int, int)> snake = new(); // 16x16
    
    public Snake()
    {
        for (int i = 0; i < 5; i++)
        {
            Inputs.Add(new Pin());
        }
        
        for (int i = 0; i < 2; i++)
        {
            Outputs.Add(new Pin(this));
        }
        
        Name = "SNAKE\nCPU";
        
        reset();
    }
    
    public void reset()
    {
        snake.Clear();
        
        snake.Enqueue((8, 8));
        snake.Enqueue((8, 9));
        snake.Enqueue((8, 10));
    }

    private int direction;
    public bool move()
    {
        // 0 - UP, 1 - DOWN, 2 - LEFT, 3 - RIGHT
        (int, int) head = snake.Last();
        
        if (direction == 0)
        {
            snake.Enqueue((head.Item1, head.Item2 - 1));
        }
        else if (direction == 1)
        {
            snake.Enqueue((head.Item1, head.Item2 + 1));
        }
        else if (direction == 2)
        {
            snake.Enqueue((head.Item1 - 1, head.Item2));
        }
        else if (direction == 3)
        {
            snake.Enqueue((head.Item1 + 1, head.Item2));
        }
        
        snake.Dequeue();
        
        // Check if the snake has hit itself
        for (int i = 0; i < snake.Count - 1; i++)
        {
            if (snake.ElementAt(i) == head)
            {
                return false;
            }
        }
        
        return true;
    }
    
    bool resetFlag = false;
    
    public override void Update()
    {
        if (Inputs[4].State)
        {
            reset();
            // Output 0 until the counter reaches 0
            if (counter == 0)
            {
                Outputs[0].State = false;
                Outputs[1].State = false;
                resetFlag = true;
            }
            else
            {
                Outputs[0].State = true;
                Outputs[1].State = false;
                counter--;
            }   
            return;
        }
        else if (resetFlag)
        {
            resetFlag = false;
            counter = 256;
        }
        
        counter--;

        if (counter == 0)
        {
            // Get input
            // 0 - UP
            // 1 - DOWN
            // 2 - LEFT
            // 3 - RIGHT
            if (Inputs[0].State)
            {
                direction = 0;
            }
            else if (Inputs[1].State)
            {
                direction = 1;
            }
            else if (Inputs[2].State)
            {
                direction = 2;
            }
            else if (Inputs[3].State)
            {
                direction = 3;
            }
            
            // Move the snake
            // if (!move())
            // {
            //     reset();
            // }
            
            move();
            
            counter = 255;
        }

        Outputs[0].State = true;
        
        // Output the game state... For each cell, output 0000000 if empty, 0100000 if snake or food starting from the second output
        // Get the current cell according to the counter (going in reverse, so 255 is 0x0, 254 is 0x1, etc.)
        int cell = 255 - counter;
        
        int x = cell % 16;
        int y = cell / 16;
        
        // Get if it's contained in the snake
        bool snakeCell = false;
        
        foreach ((int, int) segment in snake)
        {
            if (segment == (x, y))
            {
                snakeCell = true;
                break;
            }
        }
        
        if (snakeCell)
        {
            Outputs[1].State = false;
        }
        else
        {
            Outputs[1].State = true;
        }
    }
}