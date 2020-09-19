using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Zenith.Models
{
    public static class PropertyChangedNotificationInterceptor
    {
        public static void Intercept(object target, Action onPropertyChangedAction, string propertyName)
        {
            if (Application.Current == null) throw new OperationCanceledException("Main windows was closed");
            Application.Current.Dispatcher.Invoke(onPropertyChangedAction);
        }
    }
}
