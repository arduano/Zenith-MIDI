using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Zenith.Models
{
    public static class Err
    {
        static void HandleUIError(UIException e)
        {
            Console.WriteLine("Incorrect action");
            Console.WriteLine(e.Message);
        }

        static void HandleUnknownError(Exception e)
        {
            if (e is AggregateException)
            {
                foreach (var ce in (e as AggregateException).InnerExceptions)
                {
                    HandleUnknownError(ce);
                }
                return;
            }
            if (e is TargetInvocationException)
            {
                HandleUnknownError((e as TargetInvocationException).InnerException);
                return;
            }
            Console.WriteLine("An unknown error occured");
            Console.WriteLine(e);
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
            catch (OperationCanceledException e)
            {
                throw e;
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
