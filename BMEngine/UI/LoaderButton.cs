using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZenithEngine.UI
{
    public class LoadableRoutedEventArgs : RoutedEventArgs
    {
        object l = new object();
        bool hasLoaded = false;
        Action loadCallback;

        public void Loaded()
        {
            lock (l)
            {
                if (hasLoaded) return;
                hasLoaded = true;
                loadCallback();
            }
        }

        public LoadableRoutedEventArgs(RoutedEvent e, Action loadCallback, object source) : base(e, source)
        {
            this.loadCallback = loadCallback;
        }
    }

    public delegate void LoadableRoutedEventHandler(object sender, LoadableRoutedEventArgs e);

    public class LoaderButton : Button
    {
        public bool Loading
        {
            get { return (bool)GetValue(LoadingProperty); }
            set { SetValue(LoadingProperty, value); }
        }

        public static readonly DependencyProperty LoadingProperty =
            DependencyProperty.Register("Loading", typeof(bool), typeof(LoaderButton), new PropertyMetadata(false));


        public bool LoadingInternal
        {
            get { return (bool)GetValue(LoadingInternalProperty); }
            set { SetValue(LoadingInternalProperty, value); }
        }

        private static readonly DependencyProperty LoadingInternalProperty =
            DependencyProperty.Register("LoadingInternal", typeof(bool), typeof(LoaderButton), new PropertyMetadata(false));


        public bool ShowingLoader
        {
            get { return (bool)GetValue(ShowingLoaderProperty); }
            set { SetValue(ShowingLoaderProperty, value); }
        }

        public static readonly DependencyProperty ShowingLoaderProperty =
            DependencyProperty.Register("ShowingLoader", typeof(bool), typeof(LoaderButton), new PropertyMetadata(false));


        public bool RaiseLoaderClick
        {
            get { return (bool)GetValue(RaiseLoaderClickProperty); }
            set { SetValue(RaiseLoaderClickProperty, value); }
        }

        public static readonly DependencyProperty RaiseLoaderClickProperty =
            DependencyProperty.Register("RaiseLoaderClick", typeof(bool), typeof(LoaderButton), new PropertyMetadata(true));


        public static readonly RoutedEvent LoaderClickEvent = EventManager.RegisterRoutedEvent(
            "LoaderClick", RoutingStrategy.Bubble,
            typeof(LoadableRoutedEventHandler), typeof(Checkbox));

        public event LoadableRoutedEventHandler LoaderClick
        {
            add { AddHandler(LoaderClickEvent, value); }
            remove { RemoveHandler(LoaderClickEvent, value); }
        }

        public LoaderButton()
        {
            new InplaceConverter<bool, bool, bool>(
                new BBinding(LoadingProperty, this),
                new BBinding(LoadingInternalProperty, this),
                (l1, l2) => 
                    l1 || l2
                )
                .Set(this, ShowingLoaderProperty);

            Click += LoaderButton_Click;
        }

        private void LoaderButton_Click(object sender, RoutedEventArgs e)
        {
            if (RaiseLoaderClick)
            {
                if (LoadingInternal) return;
                LoadingInternal = true;
                RaiseEvent(new LoadableRoutedEventArgs(LoaderClickEvent, () => { LoadingInternal = false; }, this));
            }
        }
    }
}
