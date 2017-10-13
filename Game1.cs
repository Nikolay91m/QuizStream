using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XNAC = Microsoft.Xna.Framework.Color;
using System.Drawing;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3;
using System.Threading;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using YoutubeTest;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Util;

namespace QuizStream
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        public const string ruscaps = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        public const string rusnoncaps = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        int maxspritesize = 230400;
        int globalheight = 720;

        SpriteFont Font1;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D texture;
        FontRenderer fontRendererBlack, fontRendererWhite, fontRendererBlue;
        string lastuser, lastscore, lastgame1, lastgame2;
        int numberOfImages;
        int maxtimer = 2000; int midtimer = 600; //Таймер для появления результата и таблицы лидеров
        int[] xpng;
        int[] ypng;
        int[] ypngfrom;
        int[] xpngfrom;
        string[] config;
        string twitchchannel;
        int[] timerpng;
        XNAC[] colorpng;
        Vector2[] bezier0;
        Vector2[] bezier1;
        Vector2[] bezier2;
        Vector2[] bezier3;
        XNAC[] texturecolors;
        XNAC backgroundXNAC = new XNAC(0, 0, 0, 255);
        bool authorized = false;
        int[] shuffle = new int[320];
        int[] shuffleimages; // = new int[numberOfImages];
        int width, height, size, center;
        float speeddraw = 1; int speeddraw2 = 1;
        int imgnumber = 0; int shufflednumber = 0;
        float frame = 0;
        bool backgroundisbright = false;
        bool drawnewimage = false;
        int order;
        int ordery;
        int waittimer = -1;
        int waitfornextanswer = 600;
        int memberid = 0;
        int pixelcounter;
        int leaderboardscroll = 0;
        MouseState currentMouseState;
        MouseState previousMouseState;
        System.Drawing.Color pixel;
        ChatSharp.IrcUser user;
        ChatSharp.IrcClient client;
        List<ReceivedMessage> receivedmessages = new List<ReceivedMessage>();
        List<Member> members = new List<Member>();
        List<Member> leaders = new List<Member>();
        List<string> validanswers;
        List<string> imagestxt;
        List<string> gamewords = new List<string>();
        List<int> speeddrawmodes = new List<int>();
        List<float> speeddrawsettings = new List<float>();
        bool ready = false;
        bool[] prevstateofskip = new bool[14];
        bool debugmode = false; string debugnumber = "";
        int frameswithoutmessage = 0;
        bool reconnect = false;
        bool randomorder = true;
        bool waitmode = true;

        public YouTubeService youTubeService;
        public string liveChatId;
        public bool updatingChat;
        public int prevResultCount;
        public List<string> MessagesId = new List<string>();
        public bool Connected { get; private set; }
        int ChatUpdated = -1;
        LiveChatMessageListResponse livechatResponse;
        LiveChatMessagesResource.ListRequest livechat;

        public Game1()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        private Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float tt = t*t;
            float uu = u*u;
            float uuu = uu * u;
            float ttt = tt * t;
 
            Vector2 p = uuu * p0;    //first term
            p += 3 * uu * t * p1;    //second term
            p += 3 * u * tt * p2;    //third term
            p += ttt * p3;           //fourth term
 
            return p;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override async void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            var membersfromfile = File.ReadAllLines(@"leaderboard.txt");
            foreach (var memberfromfile in membersfromfile)
            {
                Member membersfind = members.Find(x => x.youtubeID.Contains(memberfromfile.Split('|')[1]));
                if (membersfind == null)
                {
                    members.Add(
                        new Member(
                            memberfromfile.Split('|')[0], memberfromfile.Split('|')[1],
                            memberid, Convert.ToInt32(memberfromfile.Split('|')[2])
                            )
                            );
                    memberid++;
                }
                else
                {
                    members[membersfind.id].AddScore(Convert.ToInt32(memberfromfile.Split('|')[2]));
                }
            }

            Font1 = Content.Load<SpriteFont>("Test");
            // TODO: use this.Content to load your game content here

            imagestxt = File.ReadAllLines(@"Images\List.txt").ToList();
            List<string> error = new List<string>();
            for (int i = 0; i < imagestxt.Count(); i++)
            {
                if (!File.Exists(@"Images\" + imagestxt[i].Split('|')[0]))
                {
                    error.Add(imagestxt[i].Split('|')[0] + @" not found.");
                    imagestxt.RemoveAt(i);
                    i--;
                }
            }
            File.WriteAllLines(@"log.txt", error);
            config = File.ReadAllLines(@"config.txt").ToArray();
            foreach (string configline in config)
            {
                if (configline.Contains(@":"))
                {
                    if (configline.Split(':')[0].Trim() == "Twitch channel")
                    {
                        twitchchannel = configline.Split(':')[1].Trim();
                    }
                    if (configline.Split(':')[0].Trim() == "DEBUGMODE" && configline.Split(':')[1].Trim() == "1")
                    {
                        debugmode = true;
                    }
                    if (configline.Split(':')[0].Trim()[0] == 'S' && configline.Split(':')[0].Trim()[1] == '_')
                    {
                        if (configline.Split(':')[0].Trim() == "S_DEFAULT")
                        {
                            speeddrawmodes.Add(0);
                            speeddrawsettings.Add(Convert.ToSingle(configline.Split(':')[1].Trim()));
                        }
                        else
                        {
                            string temp = configline.Split(':')[0].Trim().Remove(0, 2);
                            speeddrawmodes.Add(Convert.ToInt32(temp));
                            speeddrawsettings.Add(Convert.ToSingle(configline.Split(':')[1].Trim()));
                        }
                    }
                    if (configline.Split(':')[0].Trim() == "Height window")
                    {
                        globalheight = Convert.ToInt32(configline.Split(':')[1].Trim());
                    }
                    if (configline.Split(':')[0].Trim() == "Wait for next answer")
                    {
                        waitfornextanswer = Convert.ToInt32(configline.Split(':')[1].Trim());
                    }
                    if (configline.Split(':')[0].Trim() == "Wait after right answer")
                    {
                        maxtimer = midtimer + Convert.ToInt32(configline.Split(':')[1].Trim());
                    }
                    if (configline.Split(':')[0].Trim() == "Random order")
                    {
                        if (configline.Split(':')[1].Trim() == "1") randomorder = true;
                        else randomorder = false;
                    }
                    if (configline.Split(':')[0].Trim() == "Wait mode")
                    {
                        if (configline.Split(':')[1].Trim() == "1")
                        {
                            waitmode = true;
                        }
                        else waitmode = false;
                    }
                }
            }

            graphics.PreferredBackBufferWidth = 320;
            graphics.PreferredBackBufferHeight = globalheight;
            graphics.SynchronizeWithVerticalRetrace = true;
            graphics.ApplyChanges();

            //Sort speed modes
            for (int i = 0; i < speeddrawmodes.Count(); i++)
            {
                for (int j = i + 1; j < speeddrawmodes.Count(); j++)
                {
                    if (speeddrawmodes[i] > speeddrawmodes[j])
                    {
                        int temp;
                        temp = speeddrawmodes[j];
                        speeddrawmodes[j] = speeddrawmodes[i];
                        speeddrawmodes[i] = temp;
                        float temp2;
                        temp2 = speeddrawsettings[j];
                        speeddrawsettings[j] = speeddrawsettings[i];
                        speeddrawsettings[i] = temp2;
                    }
                }
            }

            maxspritesize = 320 * globalheight;
            xpng = new int[maxspritesize];
            ypng = new int[maxspritesize];
            ypngfrom = new int[maxspritesize];
            xpngfrom = new int[maxspritesize];
            timerpng = new int[maxspritesize];
            colorpng = new XNAC[maxspritesize];
            bezier0 = new Vector2[maxspritesize];
            bezier1 = new Vector2[maxspritesize];
            bezier2 = new Vector2[maxspritesize];
            bezier3 = new Vector2[maxspritesize];
            texturecolors = new XNAC[maxspritesize];

            numberOfImages = imagestxt.Count();

            var fontFilePath = Path.Combine(Content.RootDirectory, "Arcade.fnt");
            using (var stream = TitleContainer.OpenStream(fontFilePath))
            {
                var fontFile = FontLoader.Load(stream);
                //var fontTexture = Content.Load<Texture2D>("Arcade_Black.png");
                //fontRendererBlack = new FontRenderer(fontFile, fontTexture);
                //fontTexture = Content.Load<Texture2D>("Arcade_White.png");
                //fontRendererWhite = new FontRenderer(fontFile, fontTexture);
                //fontTexture = Content.Load<Texture2D>("Arcade_Blue.png");
                //fontRendererBlue = new FontRenderer(fontFile, fontTexture);
                stream.Close();
            }

            List<int> shufnums = new List<int>();
            for (int i = 0; i < numberOfImages; i++) { shufnums.Add(i); }
            if (randomorder)
            {
                Random rng = new Random();
                int n = shufnums.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    int value = shufnums[k];
                    shufnums[k] = shufnums[n];
                    shufnums[n] = value;
                }
            }
            shuffleimages = shufnums.ToArray();

            currentMouseState = Mouse.GetState();
            texture = new Texture2D(GraphicsDevice, 320, globalheight);
            await InitIRC();
            LoadNewPNG();
            ClearTexture();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(XNAC.CornflowerBlue);

            if (ready) Go();
            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                if (!prevstateofskip[13])
                {
                    if (ready) ready = false;
                    else
                    {
                        receivedmessages.Clear();
                        ready = true;
                    }
                    prevstateofskip[13] = true;
                }
            }
            else { prevstateofskip[13] = false; }
            if (debugmode)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    if (!prevstateofskip[0])
                    {
                        Random rng = new Random();
                        int name = rng.Next(0, 1000000000);
                        prevstateofskip[0] = true;
                        members.Add(new Member(name.ToString(), "11111", 999));
                        members.Last().score++;
                        lastuser = name.ToString();
                        lastgame1 = "fake";
                        lastgame2 = "fake";
                        leaders = Leaderboard();
                        waittimer = 800;
                        for (int i = 0; i < pixelcounter; i++)
                        {
                            if (timerpng[i] == -2) timerpng[i] = 100;
                        }
                    }
                }
                else { prevstateofskip[0] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D0))
                {
                    if (!prevstateofskip[1])
                    {
                        debugnumber += "0"; prevstateofskip[1] = true;
                    }
                }
                else { prevstateofskip[1] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D1))
                {
                    if (!prevstateofskip[2])
                    {
                        debugnumber += "1"; prevstateofskip[2] = true;
                    }
                }
                else { prevstateofskip[2] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D2))
                {
                    if (!prevstateofskip[3])
                    {
                        debugnumber += "2"; prevstateofskip[3] = true;
                    }
                }
                else { prevstateofskip[3] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D3))
                {
                    if (!prevstateofskip[4])
                    {
                        debugnumber += "3"; prevstateofskip[4] = true;
                    }
                }
                else { prevstateofskip[4] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D4))
                {
                    if (!prevstateofskip[5])
                    {
                        debugnumber += "4"; prevstateofskip[5] = true;
                    }
                }
                else { prevstateofskip[5] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D5))
                {
                    if (!prevstateofskip[6])
                    {
                        debugnumber += "5"; prevstateofskip[6] = true;
                    }
                }
                else { prevstateofskip[6] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D6))
                {
                    if (!prevstateofskip[7])
                    {
                        debugnumber += "6"; prevstateofskip[7] = true;
                    }
                }
                else { prevstateofskip[7] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D7))
                {
                    if (!prevstateofskip[8])
                    {
                        debugnumber += "7"; prevstateofskip[8] = true;
                    }
                }
                else { prevstateofskip[8] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D8))
                {
                    if (!prevstateofskip[9])
                    {
                        debugnumber += "8"; prevstateofskip[9] = true;
                    }
                }
                else { prevstateofskip[9] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.D9))
                {
                    if (!prevstateofskip[10])
                    {
                        debugnumber += "9"; prevstateofskip[10] = true;
                    }
                }
                else { prevstateofskip[10] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.Back))
                {
                    if (!prevstateofskip[11])
                    {
                        prevstateofskip[11] = true;
                        if (debugnumber.Length > 0)
                        {
                            debugnumber = debugnumber.Remove(debugnumber.Length - 1);
                        }
                    }
                }
                else { prevstateofskip[11] = false; }
                if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                {
                    if (!prevstateofskip[12])
                    {
                        prevstateofskip[12] = true;
                        if (debugnumber.Length > 0)
                        {
                            int numberofimage = Convert.ToInt32(debugnumber) - 1;
                            if (numberofimage > numberOfImages) debugnumber = "";
                            else
                            {
                                imgnumber = numberofimage;
                                LoadNewPNG();
                            }
                        }
                    }
                }
                else { prevstateofskip[12] = false; }
            }
            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
            spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Rectangle(0, 0, 320, globalheight), XNAC.White);
            if (waittimer == -1)
            {
                //DrawTextWithShadow(160, 10, "УГАДАЙКА!", true);
                DrawTextWithShadow(160, 10, "Угадай игру по персонажу,", true);
                DrawTextWithShadow(160, 35, "напечатав в чат", true);
                DrawTextWithShadow(160, 60, "!название игры", true);
            }
            else if (waittimer > midtimer)
            {
                DrawTextWithShadow(8, 10, "Угадал:", false);
                if (lastuser.Count() > 19) DrawTextWithShadow(8, 26, lastuser.Substring(0, 16) + "...", false);
                else DrawTextWithShadow(8, 35, lastuser, false);
                DrawTextWithShadow(8, 60, "Игра:", false);
                DrawTextWithShadow(8, 85, lastgame1, false);
                DrawTextWithShadow(8, 110, lastgame2, false);
                leaderboardscroll = 0;
                previousMouseState = Mouse.GetState();
                currentMouseState = Mouse.GetState();
            }
            else
            {
                previousMouseState = currentMouseState;
                currentMouseState = Mouse.GetState();
                if (currentMouseState.ScrollWheelValue < previousMouseState.ScrollWheelValue)
                {
                    leaderboardscroll++;
                    if (leaderboardscroll > leaders.Count() - 22) leaderboardscroll = leaders.Count() - 22;
                }
                else if (currentMouseState.ScrollWheelValue > previousMouseState.ScrollWheelValue) leaderboardscroll--;
                if (leaderboardscroll < 0) leaderboardscroll = 0;
                //RebootIRC();
                DrawTextWithShadow(160, 10, "Таблица лидеров", true);
                int max = leaders.Count();
                if (max > 21) max = 21;
                int number = 1;
                //if (MouseStat)
                for (int i = 0; i < leaders.Count(); i++)
                {
                    if (leaders[i].score > 0)
                    {
                        if (i > 0)
                        {
                            if (!(leaders[i].score == leaders[i - 1].score)) number = i + 1;
                        }
                        if (i >= leaderboardscroll && i <= leaderboardscroll + max)
                        {
                            DrawTextWithShadow(8, 40 + (i-leaderboardscroll) * 20, number.ToString() + ".", false);
                            int wordlength = leaders[i].member.Split('|')[0].Length;
                            if (wordlength > 20) wordlength = 20;
                            int wordalign = (600 - waittimer) / 30;
                            if (wordalign > leaders[i].member.Split('|')[0].Length - wordlength) wordalign = leaders[i].member.Split('|')[0].Length - wordlength;
                            DrawTextWithShadow(40, 40 + (i - leaderboardscroll) * 20, leaders[i].member.Split('|')[0].Substring(wordalign, wordlength), false);
                            DrawTextWithShadow(288, 40 + (i - leaderboardscroll) * 20, leaders[i].score.ToString(), true);
                        }
                    }
                }
                if (imgnumber == numberOfImages)
                {
                    DrawTextWithShadow(160, 640, "Угадайка окончена!", true);
                    DrawTextWithShadow(160, 665, "Спасибо за участие!", true);
                }
            }
            if (waittimer == 50)
            {
                if (imgnumber == numberOfImages || waitmode) { ready = false; waittimer--; }
            }
            spriteBatch.End();

            string WindowTitle = "";
            if (!ready) WindowTitle = "Пауза! ";
            if (debugmode) WindowTitle += "Вводи номер: " + debugnumber + " - " + (1000 / gameTime.ElapsedGameTime.Milliseconds).ToString();
            else { WindowTitle += "Номер картинки: " + imgnumber + " - " + (1000 / gameTime.ElapsedGameTime.Milliseconds).ToString(); }
            this.Window.Title = WindowTitle;

            if (ready)
            {
                frameswithoutmessage++;
                if (frameswithoutmessage == 3600)
                {
                    //client.SendRawMessage(string.Format("PRIVMSG {0} :{1}", twitchchannel, "!боярский"));
                }
                else if (frameswithoutmessage == 6000)
                {
                    //RebootIRC();
                }
            }

            //this.Window.Title = frameswithoutmessage.ToString();

            if (ChatUpdated == -1)
            {
                ChatUpdated = -2;
                UpdateChat();
            }
            else if (ChatUpdated > -1) ChatUpdated -= 1;

            base.Draw(gameTime);
        }

        public void Go()
        {
            for (int i = 0; i < members.Count(); i++ )
            {
                if (members[i].tries > 0) members[i].tries--;
            }
            frame++;
            if (frame > speeddraw)
            {
                frame-=speeddraw;
                for (int k = 0; k < speeddraw2; k++) //Добавляются новые пиксели для отрисовки
                {
                    if (order < pixelcounter)
                    {
                        order++;
                        if (timerpng[order] == -2) timerpng[order] = 100;
                    }
                }
            }

            for (int i = 0; i < receivedmessages.Count; i++)
            {
                string message = receivedmessages[i].message.ToLower();
                bool success = false;
                List<Regex> regexes = new List<Regex>();
                if (message.Count() > 3)
                {
                    if (message[0] == '!')
                    {
                        message = message.Replace("!", "").Replace("the ", "").Replace("&", " & ").Replace("  ", " ").Replace("зе ", "").Replace(@"'", "").Replace(".", "").Replace("?", "").Replace(@"/", "").Replace(@"\", "").Replace(@"{", "").Replace(@"}", "").Replace(@"^", "").Replace(@"$", "").Replace(@",", "").Replace(@"[", "").Replace(@"]", "").Replace(@":", "").Replace(@"+", "").Replace(@"<", "").Replace(@">", "").Replace(@"*", "").Replace(@"(", "").Replace(@")", "").Replace("&", "and").Trim();
                        Member membersfind = members.Find(x => x.youtubeID.Contains(receivedmessages[i].youtubeID));
                        bool disallowedtoanswer = false;
                        if (membersfind == null)
                        {
                            members.Add(new Member(receivedmessages[i].user, receivedmessages[i].youtubeID, memberid));
                            members[memberid].tries = waitfornextanswer;
                            memberid++;
                        }
                        else
                        {
                            if (members[membersfind.id].tries == 0) members[membersfind.id].tries = waitfornextanswer;
                            else
                            {
                                //client.SendRawMessage(string.Format("PRIVMSG {0} :{1}", twitchchannel, "@" + members[membersfind.id].member + ", вы слишком часто пытаетесь угадывать, подождите " + (members[membersfind.id].tries / 60 + 1).ToString() + " секунд."));
                                disallowedtoanswer = true;
                            }
                        }
                        if (!disallowedtoanswer)
                        {
                            foreach (string validanswer in validanswers)
                            {
                                string validanswertocheck = validanswer.ToLower().Replace("!", "").Replace("the ", "").Replace("  ", " ").Replace("зе ", "").Replace(@"'", "").Replace(".", "").Replace("?", "").Replace(@"/", "").Replace(@"\", "").Replace(@"{", "").Replace(@"}", "").Replace(@"^", "").Replace(@"$", "").Replace(@",", "").Replace(@"[", "").Replace(@"]", "").Replace(@":", "").Replace(@"+", "").Replace(@"<", "").Replace(@">", "").Replace(@"*", "").Replace(@"(", "").Replace(@")", "").Trim();
                                if (validanswertocheck.Length > 5)
                                {
                                    for (int j = 0; j < validanswertocheck.Count(); j++)
                                    {
                                        bool isValid = char.IsLetter(validanswertocheck[j]);
                                        if (isValid)
                                        {
                                            string regexmessage = validanswertocheck;
                                            regexmessage = regexmessage.Remove(j, 1);
                                            regexmessage = regexmessage.Insert(j, @"\D?\D?");
                                            try
                                            {
                                                regexes.Add(new Regex(@"^" + regexmessage.Replace("&", " & ").Replace("  ", " ").Replace("&", "and") + @"$"));
                                            }
                                            catch
                                            {

                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    regexes.Add(new Regex(@"^" + validanswertocheck.Replace("&", " & ").Replace("  ", " ").Replace("&", "and") + @"$"));
                                }

                            }
                            if (regexes.Count() > 0)
                            {
                                foreach (Regex regex in regexes)
                                {
                                    if (regex.Match(message).Success) { success = true; break; }
                                }
                            }
                        }
                    }
                }
                if (waittimer == -1 && success)
                {
                    waittimer = maxtimer;
                    drawnewimage = true;
                    Member membersfind = members.Find(x => x.youtubeID.Contains(receivedmessages[i].youtubeID));
                    if (membersfind == null)
                    {
                        members.Add(new Member(receivedmessages[i].user, receivedmessages[i].youtubeID, memberid));
                        members[memberid].score++;
                        memberid++;
                        lastuser = receivedmessages[i].user;
                        lastscore = members[memberid - 1].score.ToString();
                    }
                    else
                    {
                        members[membersfind.id].score++;
                        lastuser = members[membersfind.id].member = receivedmessages[i].user;
                        lastscore = members[membersfind.id].score.ToString();
                    }
                    lastgame1 = ""; lastgame2 = "";
                    int counterforgamewords = 0;
                    for (int j = 0; j < gamewords.Count(); j++)
                    {
                        counterforgamewords += gamewords[j].Count();
                        counterforgamewords++;
                        if (counterforgamewords > 23) { lastgame2 += gamewords[j]; lastgame2 += " "; }
                        else { lastgame1 += gamewords[j]; lastgame1 += " "; }
                    }
                }
                receivedmessages.RemoveAt(i);
                i--;
            }
            if (waittimer == maxtimer)
            {
                for (int i = 0; i < pixelcounter; i++)
                {
                    if (timerpng[i]==-2) timerpng[i] = 100;
                }
                leaders = Leaderboard();
                List<string> leaderstxt = new List<string>();
                foreach (var leader in leaders)
                {
                    leaderstxt.Add(leader.member + "|" + leader.youtubeID + "|" + leader.score.ToString());
                }
                File.WriteAllLines(@"leaderboard.txt", leaderstxt);
            }
            if (waittimer > 0)
            {
                waittimer--;
            }
            if (drawnewimage && waittimer == 0) LoadNewPNG();
            ClearTexture();
        }
        public void DrawTextWithShadow(int x, int y, string text, bool center)
        {
            if (text != null)
            {
                XNAC ColorOfText;
                if (backgroundisbright) ColorOfText = XNAC.Black;
                else ColorOfText = XNAC.White;
                Vector2 stringline = Font1.MeasureString(text) / 2;
                if (center)
                {
                    spriteBatch.DrawString(Font1, text, new Vector2(x - stringline.X, y), ColorOfText);
                }
                else
                {
                    spriteBatch.DrawString(Font1, text, new Vector2(x, y), ColorOfText);
                }
                //if (backgroundisbright) fontRendererBlack.DrawText(spriteBatch, x, y, text, 2);
                //else fontRendererBlue.DrawText(spriteBatch, x, y, text, 2);
            }
        }

        public void DrawSmallTextWithShadow(int x, int y, string text)
        {
            if (backgroundisbright) fontRendererBlack.DrawText(spriteBatch, x, y, text, 1);
            else fontRendererBlue.DrawText(spriteBatch, x, y, text, 1);
        }
        public XNAC XNAColor(System.Drawing.Color color)
        {
            return new XNAC(color.R, color.G, color.B, color.A);
        }
        public void ClearTexture()
        {
            for (int i = 0; i < maxspritesize; i++) texturecolors[i] = backgroundXNAC; //Очистка массива
            for (int i = 0; i < pixelcounter; i++ )
            {
                for (int k = 0; k < size; k++)
                { //Отрисовка пикселя
                    for (int l = 0; l < size; l++)
                    {
                        int positionsubpixel = k + ((ypng[i]) * (size)) + xpngfrom[i]
                            + l * 320 + ((xpng[i]) * (size) * 320 + ypngfrom[i] * 320) + center;
                        if (positionsubpixel > 0 && positionsubpixel < maxspritesize && 
                            (k + ((ypng[i]) * (size)) + xpngfrom[i] + center) > 0 &&
                            (k + ((ypng[i]) * (size)) + xpngfrom[i] + center) < 320)
                            texturecolors[positionsubpixel] = colorpng[i];
                    }
                }
            }
            for (int i = 0; i < pixelcounter; i++ )
            {
                if (timerpng[i] >= 0)
                {
                    float timerpngfloat = Convert.ToSingle(timerpng[i]);
                    Vector2 calculated = CalculateBezierPoint((100.0f-timerpng[i])/100, bezier0[i], bezier1[i], bezier2[i], bezier3[i]);
                    xpngfrom[i] = Convert.ToInt32(calculated.X);
                    int sizedheight = (size * height) / 2;
                    ypngfrom[i] = Convert.ToInt32(calculated.Y) + 178 - sizedheight;
                    timerpng[i]--;
                }
                if (waittimer > -1 && waittimer < midtimer) ypngfrom[i] += 10;
            }
            texture.SetData<XNAC>(texturecolors);
        }
        public void LoadNewPNG()
        {
            //Png to my format
            drawnewimage = false;
            shufflednumber = shuffleimages[imgnumber];
            waittimer = -1; //-1 - рисуется, >0 - показать таблицу лидеров, 0 - приступить к следующему рисунку
            validanswers = imagestxt[shufflednumber].Split('|').ToList();
            gamewords = validanswers[1].Split(' ').ToList();
            string link = validanswers[0];
            validanswers.RemoveAt(0);
            imgnumber++;
            Bitmap img = new Bitmap(@"Images\" + link);
            width = img.Width;
            height = img.Height;
            //speeddraw - задержка, speeddraw2 - частота рисовки
            int size1 = 320 / width;
            int size2 = (globalheight - 150) / height;
            if (size1 > size2) size = size2; else size = size1;
            center = (320 - (width * size)) / 2;
            order = 0; ordery = height-1;
            backgroundisbright = false;
            Random rng = new Random();
            int maxCoordinateXY = width; if (height > maxCoordinateXY) maxCoordinateXY = height;
            pixelcounter = 0;
            backgroundXNAC = XNAColor(img.GetPixel(0, 0));
            int colorbrightness = backgroundXNAC.R + backgroundXNAC.G + backgroundXNAC.B;
            if (colorbrightness > 384) backgroundisbright = true;

            List<int[]> coords = new List<int[]>();
            List<int[]> applied = new List<int[]>();
            List<int[]> arrayoforder = new List<int[]>();
            List<int[]> connected = new List<int[]>();
            System.Drawing.Color backgrcolor = img.GetPixel(0, 0);
            bool[,] appliedarray = new bool[width, height];
            bool[,] arrayoforderarray = new bool[width, height];
            bool[,] connectedarray = new bool[width, height];
            bool[,] coordsarray = new bool[width, height];
            for (int ari = 0; ari < width; ari++) {
                for (int arj = 0; arj < height; arj++) {
                    appliedarray[ari, arj] = false;
                    arrayoforderarray[ari, arj] = false;
                    connectedarray[ari, arj] = false;
                    if (img.GetPixel(ari, arj) != backgrcolor)
                    {
                        coords.Add(new int[] { ari, arj });
                        coordsarray[ari, arj] = true;
                    }
                }
            }
            int counter = 300;
            bool getanewpixel = true;
            while (coords.Count() > 0)
            {
                if (getanewpixel)
                {
                    counter = 300;
                    int coord = 0;
                    while (getanewpixel)
                    {
                        coord = rng.Next(0, coords.Count() - 1);
                        if (!coordsarray[coords[coord][0], coords[coord][1]]) { coords.RemoveAt(coord);
                        if (coords.Count() == 0) break;
                        }
                        else getanewpixel = false;
                    }
                    if (coords.Count() == 0) break;
                    applied.Add(coords[coord]);
                    int applyx = coords[coord][0];
                    int applyy = coords[coord][1];
                    appliedarray[applyx, applyy] = true;
                    arrayoforder.Add(coords[coord]);
                    arrayoforderarray[applyx, applyy] = true;
                    coords.RemoveAt(coord);
                    coordsarray[applyx, applyy] = false;
                }
                if (coords.Count() == 0) break;
                int countnum = applied.Count;
                int offset = 0;
                if (countnum > 50)
                {
                    countnum = 50;
                    offset = rng.Next(0, countnum - 50);
                }
                for (int apply = offset; apply < countnum; apply++)
                {
                    int applyx = applied[apply][0];
                    int applyy = applied[apply][1];
                    bool rightdir = true; bool leftdir = true; bool updir = true; bool downdir = true;
                    if (applyx > 0)
                    {
                        if (img.GetPixel(applyx - 1, applyy) == backgrcolor
                        || arrayoforderarray[applyx-1, applyy]) { leftdir = false; } }
                    else { leftdir = false; }
                    if (applyx < width - 1)
                    {
                        if (img.GetPixel(applyx + 1, applyy) == backgrcolor
                        || arrayoforderarray[applyx+1, applyy]) { rightdir = false; }
                    }
                    else { rightdir = false; }
                    if (applyy > 0)
                    {
                        if (img.GetPixel(applyx, applyy - 1) == backgrcolor
                        || arrayoforderarray[applyx, applyy-1]) { updir = false; }
                    }
                    else { updir = false; }
                    if (applyy < height - 1)
                    {
                        if (img.GetPixel(applyx, applyy + 1) == backgrcolor
                        || arrayoforderarray[applyx, applyy+1]) { downdir = false; }
                    }
                    else { downdir = false; }
                    if (!leftdir && !rightdir && !updir && !downdir)
                    {
                        appliedarray[applyx, applyy] = false;
                        applied.RemoveAt(apply);
                        countnum--;
                    }
                    else
                    {
                        if (leftdir)
                        {
                            if (!connectedarray[applyx - 1, applyy])
                            {
                                connected.Add(new int[] { applyx - 1, applyy });
                                connectedarray[applyx - 1, applyy] = true;
                            }
                        }
                        if (rightdir)
                        {
                            if (!connectedarray[applyx + 1, applyy])
                            {
                                connected.Add(new int[] { applyx + 1, applyy });
                                connectedarray[applyx + 1, applyy] = true;
                            }
                        }
                        if (updir)
                        {
                            if (!connectedarray[applyx, applyy - 1])
                            {
                                connected.Add(new int[] { applyx, applyy - 1 });
                                connectedarray[applyx, applyy - 1] = true;
                            }
                        }
                        if (downdir)
                        {
                            if (!connectedarray[applyx, applyy + 1])
                            {
                                connected.Add(new int[] { applyx, applyy + 1 });
                                connectedarray[applyx, applyy + 1] = true;
                            }
                        }
                    }
                }
                if (connected.Count() > 0)
                {
                    int chosenpixel = rng.Next(0, connected.Count());
                    int applyx = connected[chosenpixel][0];
                    int applyy = connected[chosenpixel][1];
                    applied.Add(connected[chosenpixel]);
                    arrayoforder.Add(connected[chosenpixel]);
                    arrayoforderarray[applyx, applyy] = true;
                    //int[] deletableArray = connected[chosenpixel];
                    //var numOfItem = coords.FindIndex(x=>x == deletableArray);
                    coordsarray[applyx, applyy] = false;
                    connected.RemoveAt(chosenpixel);
                    connectedarray[applyx, applyy] = false;
                }
                else counter = 1;

                counter--; if (counter == 0) getanewpixel = true;
            }

            for (int i = 0; i < arrayoforder.Count(); i++ )
            {
                int drawingPixelX = arrayoforder[i][0];
                int drawingPixelY = arrayoforder[i][1];

                XNAC thisxnacolor = XNAColor(img.GetPixel(drawingPixelX, drawingPixelY));
                if (thisxnacolor != backgroundXNAC)
                {
                    colorpng[pixelcounter] = thisxnacolor;
                    xpng[pixelcounter] = drawingPixelY;
                    ypng[pixelcounter] = drawingPixelX;
                    ypngfrom[pixelcounter] = -700;
                    xpngfrom[pixelcounter] = 0;
                    bezier0[pixelcounter] = new Vector2(rng.Next(-320 / size, 320 / size), -320);
                    bezier1[pixelcounter] = new Vector2(-160, rng.Next(-64, 64));
                    bezier2[pixelcounter] = new Vector2(rng.Next(-160, 160), 32);
                    bezier3[pixelcounter] = new Vector2(0, 100);
                    timerpng[pixelcounter] = -2;
                    pixelcounter++;
                }
            }

            for (int i = 0; i < speeddrawmodes.Count(); i++ )
            {
                if (pixelcounter > speeddrawmodes[i]) speeddraw = speeddrawsettings[i];
            }

            order = 0;
            timerpng[order] = 100;
        }
        public List<Member> Leaderboard()
        {
            List<Member> leaderboard = new List<Member>(members);
            for (int i = 0; i<leaderboard.Count()-1; i++)
            {
                for (int j = i; j<leaderboard.Count(); j++)
                {
                    if (leaderboard[i].score < leaderboard[j].score)
                    {
                        Member tempmember = leaderboard[i];
                        leaderboard[i] = leaderboard[j];
                        leaderboard[j] = tempmember;
                    }
                }
            }
            return leaderboard;
        }

        public async Task InitIRC()
        {
            UserCredential credential;
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows for full read/write access to the
                    // authenticated user's account.
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }

            if (credential.Token.IsExpired(SystemClock.Default))
            {
                if (!await credential.RefreshTokenAsync(CancellationToken.None))
                {
                    Console.WriteLine("No valid refresh token.");
                }
            }

            youTubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString()
            });

            var res = youTubeService.LiveBroadcasts.List("id,snippet,contentDetails,status");
            res.BroadcastType = LiveBroadcastsResource.ListRequest.BroadcastTypeEnum.Persistent;
            res.Mine = true;

            //res.BroadcastStatus = LiveBroadcastsResource.ListRequest.BroadcastStatusEnum.Active;
            var resListResponse = await res.ExecuteAsync();

            int currentstream = 0;
            DateTime dt = resListResponse.Items[0].Snippet.PublishedAt.Value;
            for (int i = 1; i < resListResponse.Items.Count; i++ )
            {
                DateTime dt2 = resListResponse.Items[i].Snippet.PublishedAt.Value;
                if (DateTime.Compare(dt, dt2) < 0)
                {
                    dt = dt2;
                    currentstream = i;
                }
            }

            File.WriteAllText(@"chatlog.txt", resListResponse.Items[currentstream].Snippet.LiveChatId);
            liveChatId = resListResponse.Items[currentstream].Snippet.LiveChatId;
            UpdateChat();
        }

        public async void UpdateChat()
        {
            if (!string.IsNullOrEmpty(liveChatId))
            {
                updatingChat = true;
                livechat = youTubeService.LiveChatMessages.List(liveChatId, "id,snippet,authorDetails");
                livechatResponse = await livechat.ExecuteAsync();

                foreach (var livemessage in livechatResponse.Items)
                {
                    string id = livemessage.Id;
                    string displayName = livemessage.AuthorDetails.DisplayName;
                    string channelId = livemessage.AuthorDetails.ChannelId;
                    string message = livemessage.Snippet.DisplayMessage;

                    if (!MessagesId.Contains(id))
                    {
                        MessagesId.Add(id);
                        receivedmessages.Add(new ReceivedMessage(message, channelId, displayName));
                        Console.WriteLine(message);
                        File.AppendAllText("chatlog.txt", displayName + "|" + channelId + ":" + message + Environment.NewLine);
                    }
                }
            }
            ChatUpdated = 30;
            Console.WriteLine("Updated!");
        }

        //public void InitIRC()
        //{
        //    authorized = false;
        //    user = new ChatSharp.IrcUser("KrknGameBot", "KrknGameBot", "oauth:gk5gcyh5oyjgp1ey487991x563xq8c");
        //    client = new ChatSharp.IrcClient(@"irc.twitch.tv", user);
        //    client.NetworkError += (s, e) => Console.WriteLine("Error: " + e.SocketError);
        //    client.RawMessageRecieved += (s, e) => {
        //        Console.WriteLine("<< {0}", e.Message);
        //        if (e.Message.Contains("You are in a maze of twisty passages, all alike."))
        //        {
        //            authorized = true;
        //        }
        //    };
        //    client.RawMessageSent += (s, e) => Console.WriteLine(">> {0}", e.Message);
        //    client.UserMessageRecieved += (s, e) =>
        //    {
        //        if (e.PrivateMessage.Message.StartsWith(".join "))
        //            client.Channels.Join(e.PrivateMessage.Message.Substring(6));
        //        else if (e.PrivateMessage.Message.StartsWith(".list "))
        //        {
        //            var channel = client.Channels[e.PrivateMessage.Message.Substring(6)];
        //            var list = channel.Users.Select(u => u.Nick).Aggregate((a, b) => a + "," + b);
        //            client.SendMessage(list, e.PrivateMessage.User.Nick);
        //        }
        //        else if (e.PrivateMessage.Message.StartsWith(".whois "))
        //            client.WhoIs(e.PrivateMessage.Message.Substring(7), null);
        //        else if (e.PrivateMessage.Message.StartsWith(".raw "))
        //            client.SendRawMessage(e.PrivateMessage.Message.Substring(5));
        //        else if (e.PrivateMessage.Message.StartsWith(".mode "))
        //        {
        //            var parts = e.PrivateMessage.Message.Split(' ');
        //            client.ChangeMode(parts[1], parts[2]);
        //        }
        //        else if (e.PrivateMessage.Message.StartsWith(".topic "))
        //        {
        //            string messageArgs = e.PrivateMessage.Message.Substring(7);
        //            if (messageArgs.Contains(" "))
        //            {
        //                string channel = messageArgs.Substring(0, messageArgs.IndexOf(" "));
        //                string topic = messageArgs.Substring(messageArgs.IndexOf(" ") + 1);
        //                client.Channels[channel].SetTopic(topic);
        //            }
        //            else
        //            {
        //                string channel = messageArgs.Substring(messageArgs.IndexOf("#"));
        //                client.GetTopic(channel);
        //            }
        //        }
        //    };
        //    client.ChannelMessageRecieved += (s, e) =>
        //    {
        //        Console.WriteLine("<{0}> {1}", e.PrivateMessage.User.Nick, e.PrivateMessage.Message);
        //        if (ready) receivedmessages.Add(new ReceivedMessage(e.PrivateMessage.Message.Replace("ACTION ", ""), e.PrivateMessage.User.Nick));
        //        Console.WriteLine(frameswithoutmessage.ToString());
        //        frameswithoutmessage = 0;
        //    };
        //    client.ChannelTopicReceived += (s, e) =>
        //    {
        //        Console.WriteLine("Received topic for channel {0}: {1}", e.Channel.Name, e.Topic);
        //    };
        //    client.ConnectAsync();
        //    while (!authorized)
        //    {
        //        System.Threading.Thread.Sleep(2000);
        //        if (!authorized)
        //        {
        //            client.Quit();
        //            client.ConnectAsync();
        //        }
        //    }
        //    client.SendRawMessage("JOIN " + twitchchannel);
        //    if (reconnect)
        //    {
        //        reconnect = false;
        //        frameswithoutmessage = 0;
        //    }
        //}

        void RebootIRC()
        {
            ready = false;
            reconnect = true;
            this.Window.Title = "Чат отрубился. Переподключение!";
            InitIRC();
        }

        public class FontRenderer
        {
            public FontRenderer(FontFile fontFile, Texture2D fontTexture)
            {
                _fontFile = fontFile;
                _texture = fontTexture;
                _characterMap = new Dictionary<char, FontChar>();

                foreach (var fontCharacter in _fontFile.Chars)
                {
                    char c = (char)fontCharacter.ID;
                    _characterMap.Add(c, fontCharacter);
                }
            }

            private Dictionary<char, FontChar> _characterMap;
            private FontFile _fontFile;
            private Texture2D _texture;
            public void DrawText(SpriteBatch spriteBatch, int x, int y, string text, int scale)
            {
                int dx = x;
                int dy = y;
                foreach (char c in text)
                {
                    FontChar fc;
                    if (_characterMap.TryGetValue(c, out fc))
                    {
                        var sourceRectangle = new Microsoft.Xna.Framework.Rectangle(fc.X, fc.Y, fc.Width, fc.Height);
                        var position = new Vector2(dx + fc.XOffset, dy + fc.YOffset * scale);

                        spriteBatch.Draw(_texture, position, sourceRectangle, XNAC.White, 0, new Vector2(0, 0), scale, SpriteEffects.None, 0.0f);
                        dx += fc.XAdvance * scale;
                    }
                }
            }
        }
    }
}
