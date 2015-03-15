using System;
// este delegate e' a base para o event Move do slider
public delegate void MoveEventHandler(object source, MoveEventArgs e);

// contem os argumentos do evento move do slider
public class MoveEventArgs : EventArgs
{
    int position;
    bool validate;

    public MoveEventArgs(int newPosition) {position = newPosition;}

    public bool Validate {
        get { return validate; }
        set { validate = value; }
    }

    public int Position
    {
        get { return position; }
        set { position = value; }
    }
}

class Slider
{
    private int position;
    public event MoveEventHandler Event;

    public int Position {
        get { return position; }

        // e' este bloco que e' executado quando se move o slider
        set {
            MoveEventArgs args = new MoveEventArgs(value);
            if (Event != null) {
                Event(this, args);
                if (args.Validate) position = value;
            }
        }
    }
}

class Form
{
    static void Main() {
        Slider slider = new Slider();

        // register with the Move event
        slider.Event += slider_Move;

        slider.Position = 20;
        slider.Position = 60;
        Console.ReadLine();
    }

    // callback
    static void slider_Move(object source, MoveEventArgs e) {
        if (e.Position > 50) {
            Console.WriteLine("Posicao invalida!");
            e.Validate = false;
        }
        else {
            Console.WriteLine("Posicao valida!");
            e.Validate = true;
        }
    }
}