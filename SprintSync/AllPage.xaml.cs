using System;
using System.Collections.Generic;
using System.IO;
using MediaManager;
using MediaManager.Library;
using Plugin.FilePicker;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SprintSync
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AllPage : TabbedPage
    {

        Queue<IMediaItem> q = new Queue<IMediaItem>();
        float target = 160F;
        string test = "https://ia800605.us.archive.org/32/items/Mp3Playlist_555/AaronNeville-CrazyLove.mp3";

        public AllPage()
        {
            InitializeComponent();
        }


        //----------------------------//
        //        LIBRARY CODE        //
        //----------------------------//
        private async void Add2Library(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync();
                if (result != null)
                {
                    //API CALLS
                    
                    string localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), result.FileName.Split('.')[0]) + ".txt";
                    //string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "temp.txt");
                    //File.CreateText($"{localPath}.txt");
                    if (File.Exists(localPath)) File.WriteAllText(localPath, "helasdasderyone");
                    if (File.Exists(localPath)) 
                    {
                        var x = File.ReadAllText(localPath);
                        Console.WriteLine(x);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                // idgaf
            }
            
        }

        public void CheckForFile(object sender, EventArgs e)
        {
            var allFiles = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            foreach (string s in allFiles)
            {
                (sender as Button).Text += s;
            }
        }

        //private void ShowLibrary()
        //{
        //    var allFiles = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        //    foreach (string s in allFiles)
        //    {
        //        (sender as Button).Text = base;
        //    }


        //}

        //----------------------------//
        //          RUN CODE          //
        //----------------------------//


        //---------------------------//
        //        PLAYER CODE        //
        //---------------------------//
        private async void LoadFile(object sender, EventArgs e)
        {
            string[] fileTypes = null;
            if (Device.RuntimePlatform == Device.Android) { fileTypes = new string[] { "audio/mpeg" }; }
            if (Device.RuntimePlatform == Device.iOS) { fileTypes = new string[] { "public.audio" }; }
            if (Device.RuntimePlatform == Device.UWP) { fileTypes = new string[] { ".mp3" }; }
            var pickedFile = await CrossFilePicker.Current.PickFile(fileTypes);
            if (pickedFile != null)
            {
                var cachedFilePathName = Path.Combine(FileSystem.CacheDirectory, pickedFile.FileName);

                if (!File.Exists(cachedFilePathName))
                    File.WriteAllBytes(cachedFilePathName, pickedFile.DataArray);

                if (File.Exists(cachedFilePathName))
                {
                    // Create media item from file
                    var generatedMediaItem =
                        await CrossMediaManager.Current
                        .Extractor.CreateMediaItem(cachedFilePathName);

                    //CrossMediaManager.Current.Queue.Add(generatedMediaItem);
                    q.Enqueue(generatedMediaItem); // Add media item to queue
                }
            }


        }

        private async void Play(System.Object sender, System.EventArgs e)
        {
            if (q.Count != 0)
            {
                var item = q.Dequeue();
                //x = API stuff
                //SetSpeed(BPMtoMultiplier(x));
                //Queue<int[]> sectionQueue = new Queue<int[]>();
                //foreach (int[] ts in sectionQueue)
                //{
                //    Device.StartTimer(new TimeSpan(0, 0, ts[0]), () =>
                //    {
                //        SetSpeed(ts[1]);
                //        return false;
                //    });
                // }
                await CrossMediaManager.Current.Play(item);
            }
            else await CrossMediaManager.Current.Stop();
        }

        private async void Pause(object sender, EventArgs e)
        {
            await CrossMediaManager.Current.Pause();
        }

        private void SpeedUp(object sender, EventArgs e)
        {
            SetSpeed(CrossMediaManager.Current.Speed + 0.05F);
        }

        private void SlowDown(object sender, EventArgs e)
        {
            SetSpeed(CrossMediaManager.Current.Speed - 0.05F);
        }

        private void SetSpeed(float spd)
        {
            CrossMediaManager.Current.Speed = spd;
            SpeedLabel.Text = $"{spd:F2}x";
        }

        private float BPMtoMultiplier(float bpm, float leeway = 6F)
        {
            float[] choices = { target / 2, target, target * 2 };
            float minDiff = 1000000F;
            float closest = 0F;
            for (int z = 0; z < 3; z++)
            {
                float diff = Math.Abs(bpm - choices[z]);
                if (diff < minDiff) { minDiff = diff; closest = choices[z]; }
            }
            if (Math.Abs(closest - bpm) < leeway) return 1.0F;
            else return closest / bpm;
        }

        private async void testNav(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AllPage());
        }

    }
}
