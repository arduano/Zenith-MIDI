using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Zenith.Models
{
    public class UIException : Exception
    {
        public UIException(string message) : base(message) { }
    }

    public static class Err
    {
        static void HandleUIError(UIException e)
        {
            Console.WriteLine("Incorrect action");
            Console.WriteLine(e.Message);
        }

        static void HandleUnknownError(Exception e)
        {
            Console.WriteLine("An unknown error occured");
            Console.WriteLine(e.Message);
        }

        public static void Handle(Action call)
        {
            try
            {
                call();
            }
            catch (UIException e)
            {
                HandleUIError(e);
            }
            catch (Exception e)
            {
                HandleUnknownError(e);
            }
        }

        public static async Task Handle(Func<Task> call)
        {
            try
            {
                await call();
            }
            catch (UIException e)
            {
                HandleUIError(e);
            }
            catch (Exception e)
            {
                HandleUnknownError(e);
            }
        }

        public static void Notify(string message, string title = null)
        {
            if(title == null)
            {
                MessageBox.Show(message);
            }
            else
            {
                MessageBox.Show(message, title);
            }
        }
    }
}
