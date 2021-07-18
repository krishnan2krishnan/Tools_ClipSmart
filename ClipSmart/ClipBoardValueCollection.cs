using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClipSmart
{
    public class UserDataContext : INotifyPropertyChanged
    {
        private ObservableCollection<ClipBoardValue> valueCollection;

        public ObservableCollection<ClipBoardValue> ValueCollection
        {
            get
            {
                return valueCollection;
            }
        }
        public UserDataContext()
        {
            valueCollection = new ObservableCollection<ClipBoardValue>();
            NavigateLeft = new NavigateCommand((t) =>
            {
                return Index > 1;
            }, (object param) => 
            {
                //set the index to next value
                Index--;
            });

            NavigateRight = new NavigateCommand((t) =>
            {
                return Index!=0 && Index < valueCollection.Count;
            }, (object param) => 
            {
                //set the index to next value
                Index++;
            });
        }
        public int Index
        {
            get
            {
                //read from the current and the collection count
                if (current == null)
                    return 0;
                return valueCollection.Count - current.Index + 1;
            }
            set
            {
                //adjust the current
                if (value > valueCollection.Count || value < 1)
                    return;
                ClipBoardValue = valueCollection[valueCollection.Count - value];
            }
        }
        
        public ICommand NavigateLeft { get; internal set; }

        public ICommand NavigateRight { get; internal set; }

        private ClipBoardValue current;
        public ClipBoardValue ClipBoardValue
        {
            get
            {
                return current;
            }
            set
            {
                current = value;
                NotifyPropertyChanged("ClipBoardValue");
                NotifyPropertyChanged("Index");
            }
        }

        public void Add(ClipBoardValue message)
        {
            valueCollection.Add(message);
            ClipBoardValue = message;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
    public class ClipBoardValue
    {
        static int CounterIndex = 0;
        public ClipBoardValue()
        {
            index = ++CounterIndex;
            StoredDate = DateTime.Now.ToString("hh:mm tt") + " " + DateTime.Now.ToString("d");
        }
        string copiedMessage = string.Empty;
        public string CopiedMessage
        {
            get
            {
                return copiedMessage;
            }
            set
            {
                if (copiedMessage != value)
                {
                    copiedMessage = value;
                }
            }
        }

        private int index;

        public int Index
        {
            get { return index; }
            set
            {
                index = value;
            }
        }

        public string StoredDate { get; private set; }
    }
}
