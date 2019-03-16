using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using ImGuiNET;
using MgCoreEditor.Engine;
using MgCoreEditor.ImGUI;
using Shared.Engine.Input;
using XNA3DGizmoExample;
using Num = System.Numerics;

namespace MgCoreEditor
{
    public class Game1 : Game
    {
        GridComponent _grid;
        private Renderer.Renderer _renderer;

        private static readonly Num.Vector2 DefaultFilePickerSize = new Num.Vector2(500, 300);
        private const string FilePickerID = "###FilePicker";
        private string CurrentFolder = @"c:\";
        private string SelectedFile = string.Empty;
        private bool show = true;
        private string _menuAction = string.Empty;
        string selected = string.Empty;
        private bool _keepPopup = false;
        private bool _fopen = true;

        GraphicsDeviceManager _graphics;
        SpriteBatch spriteBatch;
        private Texture2D _icon;
        private ImGuiRenderer _imGuiRenderer;
        private Texture2D _xnaTexture;
        private IntPtr _imGuiTexture;
        // Direct port of the example at https://github.com/ocornut/imgui/blob/master/examples/sdl_opengl2_example/main.cpp
        private float f = 0.0f;

        private bool show_test_window = false;
        private bool show_another_window = false;
        private Num.Vector3 clear_color = new Num.Vector3(114f / 255f, 144f / 255f, 154f / 255f);
        private byte[] _textBuffer = new byte[100];

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.PreferMultiSampling = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        public Camera Camera { get; set; }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();
            Camera = new Camera(new Vector3(0, 5, -15), Vector3.Zero);
            _renderer = new Renderer.Renderer(Camera);
            CurrentFolder = AppContext.BaseDirectory;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _grid = new GridComponent(GraphicsDevice, 10);
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            _icon = Content.Load<Texture2D>("icon");
            // First, load the texture as a Texture2D (can also be done using the XNA/FNA content pipeline)
            _xnaTexture = Texture2D.FromStream(GraphicsDevice, GenerateImage(300, 150));

            // Then, bind it to an ImGui-friendly pointer, that we can use during regular ImGui.** calls (see below)
            _imGuiTexture = _imGuiRenderer.BindTexture(_xnaTexture);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            Input.Update(gameTime, Camera);
            _renderer.Update(gameTime);
            Engine.Engine.View = _renderer.View;
            Engine.Engine.Projection = _renderer.Projection;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here

            _grid.Draw();

            //spriteBatch.Begin();
            //spriteBatch.Draw(_icon, Vector2.Zero);
            //spriteBatch.End();


            // Call BeforeLayout first to set things up
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
            _imGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }


        protected virtual void ImGuiLayout()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New"))
                    {
                        _menuAction = "New";
                    }
                    if (ImGui.MenuItem("Open"))
                    {
                        _menuAction = "Open";
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Save"))
                    {
                        _menuAction = "Save";
                    }
                    if (ImGui.MenuItem("Save As"))
                    {

                        _menuAction = "SaveAs";

                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Quit"))
                    {
                        this.Exit();
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }

            if (_menuAction == "SaveAs")
            {

                bool result = false;
                bool _fopen = true;
                ImGui.SetNextWindowSize(DefaultFilePickerSize, ImGuiCond.FirstUseEver);
                ImGui.OpenPopup(FilePickerID);
            }

            //if (ImGui.BeginPopupModal(FilePickerID, ref _fopen, ImGuiWindowFlags.NoTitleBar) )
            if (ImGui.BeginPopupModal(FilePickerID))
            {
                var result = DrawFolder(ref selected, true);

                //int i_id = 1;
                //ImGui.InputInt("ID", ref i_id);
                //if (ImGui.Button("Save"))
                //{
                //    ImGui.CloseCurrentPopup();
                //    _menuAction = String.Empty;
                //}
                //ImGui.SameLine();
                //if (ImGui.Button("Cancel"))
                //{
                //    ImGui.CloseCurrentPopup();
                //    _menuAction = String.Empty;
                //}

                ImGui.EndPopup();
            }
        }

        private bool DrawFolder(ref string selected, bool returnOnSelection = false)
        {
            _keepPopup = true;
            ImGui.Text("Current Folder: " + CurrentFolder);
            bool result = false;

            if (ImGui.BeginChildFrame(1, new Num.Vector2(0, 220), ImGuiWindowFlags.Modal /*WindowFlags.Default*/))
            {
                DirectoryInfo di = new DirectoryInfo(CurrentFolder);
                if (di.Exists)
                {
                    if (di.Parent != null)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Num.Vector4(1, 1, 0, 1)/*RgbaFloat.Yellow.ToVector4()*/);
                        if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups))
                        {
                            CurrentFolder = di.Parent.FullName;
                        }
                        ImGui.PopStyleColor();
                    }
                    foreach (var fse in Directory.EnumerateFileSystemEntries(di.FullName))
                    {
                        if (Directory.Exists(fse))
                        {
                            string name = Path.GetFileName(fse);
                            ImGui.PushStyleColor(ImGuiCol.Text, new Num.Vector4(1, 1, 0, 1));
                            if (ImGui.Selectable(name + "/", false, ImGuiSelectableFlags.DontClosePopups))
                            {
                                CurrentFolder = fse;
                            }
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            string name = Path.GetFileName(fse);
                            bool isSelected = SelectedFile == fse;
                            if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups))
                            {
                                SelectedFile = fse;
                                if (returnOnSelection)
                                {
                                    result = true;
                                    selected = SelectedFile;
                                }
                            }
                            if (ImGui.IsMouseDoubleClicked(0))
                            {
                                result = true;
                                selected = SelectedFile;
                                ImGui.CloseCurrentPopup();
                                _menuAction = string.Empty;
                            }
                        }
                    }
                }

            }
            ImGui.EndChildFrame();


            if (ImGui.Button("Cancel"))
            {
                result = false;
                ImGui.CloseCurrentPopup();
                _menuAction = string.Empty;
            }

            if (SelectedFile != null)
            {
                ImGui.SameLine();
                if (ImGui.Button("Open"))
                {
                    result = true;
                    selected = SelectedFile;
                    ImGui.CloseCurrentPopup();
                    _menuAction = string.Empty;
                }
            }

            return result;
        }

        private static Stream GenerateImage(int width, int height)
        {
            var stream = new MemoryStream();
            var random = new Random(42);

            var bmp = new System.Drawing.Bitmap(width, height);
            var graphics = System.Drawing.Graphics.FromImage(bmp);
            graphics.Clear(System.Drawing.Color.Black);

            for (int i = 0; i < 100; i++)
            {
                var size = random.Next(10, 50);
                var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255)), random.Next(1, 4));

                graphics.DrawEllipse(pen, random.Next(0, width), random.Next(0, height), size, size);
            }

            bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);

            stream.Position = 0;

            return stream;
        }
    }
}
