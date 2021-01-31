using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MediaManager;
using MediaManager.Library;
using Plugin.FilePicker;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using sonic;
using System.Text;
using System.Threading;

namespace SprintSync
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AllPage : TabbedPage
    {
        string PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        Queue<String> q = new Queue<String>();
        float target = 160F;
        string test = "https://ia800605.us.archive.org/32/items/Mp3Playlist_555/AaronNeville-CrazyLove.mp3";

        public AllPage()
        {
            InitializeComponent();
            ShowLibrary();
            lt.Text = "<-";
            //ClearLocal();
        }


        //----------------------------//
        //        LIBRARY CODE        //
        //----------------------------//
        private async void Add2Library(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickMultipleAsync();
                if (result != null)
                {
                    //API CALLS
                    foreach (FileResult fr in result)
                    {
                        var s = await Sonic.Song.CreateAsync(fr.FullPath);
                        var dict = s.GetSectionDict();
                        List<String> sb = new List<String>();
                        foreach (int[] entry in dict)
                        {
                            sb.Add($"{entry[0]}:{entry[1]}");
                        }
                        string fileNoExt = fr.FileName.Split('.')[0];
                        string localPath = Path.Combine(PATH, fileNoExt) + ".txt";
                        File.WriteAllText(localPath, fr.FullPath + "|" + string.Join("/", sb));
                        UpdateLibrary(fileNoExt);
                    }
                    

                }
            }
            catch (Exception ex) { throw ex; } //idc 
        }

        private void bpmtest(object sender, EventArgs e) {
            (sender as Button).Text = $"{BPMtoMultiplier(88)}";
        }

        private void ClearLocal()
        {
            var allFiles = Directory.GetFiles(PATH);
            foreach (string s in allFiles)
            {
                File.Delete(s);
            }
        }

        public void CheckForFile(object sender, EventArgs e)
        {
            var allFiles = Directory.GetFiles(PATH);
            foreach (string s in allFiles)
            {
                (sender as Button).Text = File.ReadAllText(s);
            }
        }

        private void ShowLibrary()
        {
            var allFiles = Directory.GetFiles(PATH);
            foreach (string s in allFiles)
            {
                string path = File.ReadAllText(s).Split('|')[0];
                UpdateLibrary(Path.GetFileNameWithoutExtension(path));
            }
        }

        Dictionary<Button, bool> allBtns = new Dictionary<Button, bool>();
        private void UpdateLibrary(string s)
        {
            Button ent = new Button();
            var parts = s.Split('/');
            ent.CornerRadius = 10;
            ent.Text = Path.GetFileName(s);
            ent.Clicked += EntryClicked;
            ent.BorderColor = Color.FromHex("#2196F3");
            EntryList.Children.Add(ent);
            allBtns[ent] = false;
        }

        private string GetMusicFromName(string name)
        {
            return File.ReadAllText(Path.Combine(PATH, name + ".txt")).Split('|')[0];
        }

        //Queue<Button> buttonQ = new Queue<Button>();
        private async void EntryClicked(object sender, EventArgs e)
        {
            
            allBtns[(sender as Button)] = !allBtns[(sender as Button)];
            if (allBtns[(sender as Button)])
            {
                (sender as Button).BorderWidth = 5;
                q.Enqueue(GetMusicFromName((sender as Button).Text));
                //buttonQ.Enqueue(sender as Button);
            }
            else
            {
                (sender as Button).BorderWidth = 0;
                //q.Dequeue();
                //buttonQ.Dequeue(sender as Button);
            }
            
            //Play();
        }


        //----------------------------//
        //          RUN CODE          //
        //----------------------------//
        private void OnSliderValueChanged(object sender, EventArgs e)
        {
            target = (float)(sender as Slider).Value;
            TargetTempo.Text = $"{(int)target}";
        }

        private void StartRun(object sender, EventArgs e)
        {
            Play();
            this.CurrentPage = PlayerPage;
        }

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
                    //q.Enqueue(generatedMediaItem); // Add media item to queue
                }
            }


        }

        private async void Play(object sender=null)
        {
            if (q.Count != 0)
            {
                string item= q.Dequeue();
                //if (sender != null) allBtns[(sender as Button)] = false;
                string songName = Path.GetFileNameWithoutExtension(item);
                CurrentSong.Text = songName;
                string txtPath = Path.Combine(PATH, songName + ".txt");
                string sectionStr = File.ReadAllText(txtPath).Split('|')[1];
                Queue<int[]> sectionQueue = new Queue<int[]>();
                foreach (string entry in sectionStr.Split('/'))
                {
                    String[] a = entry.Split(':');
                    sectionQueue.Enqueue(new int[] { int.Parse(a[0]), int.Parse(a[1]) });
                }


                int[] ts = sectionQueue.Dequeue();
                SetSpeed(BPMtoMultiplier(ts[1]));
                //Device.StartTimer(new TimeSpan(0, 0, ts[0]), () =>  // Keep or not ??
                //{
                //    if (sectionQueue.Count != 0)
                //    {
                //        ts = sectionQueue.Dequeue();
                //        SetSpeed(BPMtoMultiplier(ts[1]));
                //        return true;
                //    }
                //    return false;
                //});
                var generatedMediaItem =
                        await CrossMediaManager.Current
                        .Extractor.CreateMediaItem(item);
                var status = await CrossMediaManager.Current.Play(generatedMediaItem);
            }
            else await CrossMediaManager.Current.Stop();
        }

        bool playing = true;
        private async void PlayPause(object sender, EventArgs e)
        {
            await CrossMediaManager.Current.PlayPause();
            playing = !playing;
            if (playing) (sender as Button).Text = "||";
            else (sender as Button).Text = ">";
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
            Device.StartTimer(new TimeSpan(0, 0, 1), () =>
            {
                CrossMediaManager.Current.Speed = spd; return false;
            });
            //Thread.Sleep(50);
            //CrossMediaManager.Current.Speed = spd;

        }

        private async void Next(object sender, EventArgs e)
        {
            Play();
        }

        private async void Last(object sender, EventArgs e)
        {
            // this doesn't work right now.
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
