using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using ImGuiNET;
using MgCoreEditor.Engine;
using MgCoreEditor.Engine.Input;
using MgCoreEditor.ImGUI;
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

        private Model _cube;
        private Effect _specularEffect;
        private Texture2D _cubeDiffuse;

        private Vector3 _modelPosition = Vector3.Zero;

        private Vector3 DiffDir = Vector3.Left;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this){GraphicsProfile = GraphicsProfile.HiDef};
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.PreferMultiSampling = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        public IEditorCamera Camera { get; set; }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();
            SetupDarkStyle();
            Camera = new PerspectiveCamera(new Vector3(-10, 5, -15), Vector3.Zero);
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

            _cube = Content.Load<Model>("models/Cube_obj");
            //_cube = Content.Load<Model>("models/xbot");
            _cubeDiffuse = Content.Load<Texture2D>("textures/Cube_diffuse");
            _specularEffect = Content.Load<Effect>("effects/Specular");

            //SetupAmbientEffect();
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

            // Draw model
            DrawModel();

            //var effect = (BasicEffect)meshPart.Effect;
            //effect.EnableDefaultLighting();
            //effect.PreferPerPixelLighting = true;
            //effect.World = transforms[mesh.ParentBone.Index] * wworld;
            //effect.View = _renderer.View;
            //effect.Projection = _renderer.Projection;

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

        private void DrawModel()
        {
            Matrix[] transforms = new Matrix[_cube.Bones.Count];
            _cube.CopyAbsoluteBoneTransformsTo(transforms);

            var viewVector = Camera.Forward;
            viewVector.Normalize();

            Matrix wworld = GetWorld(0.05f, _modelPosition);

            _specularEffect.Parameters["AmbientColor"].SetValue(Color.Yellow.ToVector4());
            _specularEffect.Parameters["AmbientIntensity"].SetValue(0.75f);
            _specularEffect.Parameters["SpecularColor"].SetValue(Color.Yellow.ToVector4());
            _specularEffect.Parameters["SpecularIntensity"].SetValue(0.1f);
            _specularEffect.Parameters["DiffuseLightDirection"].SetValue(DiffDir);
            _specularEffect.Parameters["DiffuseColor"].SetValue(Color.Red.ToVector4());
            _specularEffect.Parameters["DiffuseIntensity"].SetValue(0.1f);

            foreach (var mesh in _cube.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    //meshPart.Effect = _specularEffect;
                    _specularEffect.Parameters["World"].SetValue(transforms[mesh.ParentBone.Index] * wworld);
                    _specularEffect.Parameters["View"].SetValue(_renderer.View);
                    _specularEffect.Parameters["Projection"].SetValue(_renderer.Projection);

                    _specularEffect.Parameters["ViewVector"].SetValue(Camera.Forward);
                    Matrix worldInverseTransposeMatrix = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform * wworld));
                    _specularEffect.Parameters["WorldInverseTranspose"].SetValue(worldInverseTransposeMatrix);

                    GraphicsDevice.SetVertexBuffer(meshPart.VertexBuffer, meshPart.VertexOffset);
                    GraphicsDevice.Indices = meshPart.IndexBuffer;
                    _specularEffect.CurrentTechnique.Passes[0].Apply();
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, meshPart.StartIndex, meshPart.PrimitiveCount);
                }
                //mesh.Draw();
            }
        }

        public virtual Matrix GetWorld(float scale, Vector3 position)
        {
            var world = Matrix.Identity;
            Matrix.CreateTranslation(position);
            return Matrix.CreateScale(scale) * Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(180)  )  * world;
        }

        protected virtual void ImGuiLayout()
        {
            ImguiExample();

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

            if (_menuAction == "Open")
            {

                bool result = false;
                bool _fopen = true;
                ImGui.SetNextWindowSize(DefaultFilePickerSize, ImGuiCond.FirstUseEver);
                ImGui.OpenPopup(FilePickerID);
            }

            if (ImGui.BeginPopupModal(FilePickerID))
            {
                var result = DrawFolder(ref selected, true);

                ImGui.EndPopup();
            }
        }

        private void ImguiExample()
        {
            // 1. Show a simple window
            // Tip: if we don't call ImGui.Begin()/ImGui.End() the widgets appears in a window automatically called "Debug"
            {
                ImGui.Text("Hello, world!");
                ImGui.SliderFloat("float", ref f, 0.0f, 1.0f, string.Empty, 1f);
                ImGui.ColorEdit3("clear color", ref clear_color);
                if (ImGui.Button("Test Window")) show_test_window = !show_test_window;
                if (ImGui.Button("Another Window")) show_another_window = !show_another_window;
                ImGui.Text(string.Format("Application average {0:F3} ms/frame ({1:F1} FPS)", 1000f / ImGui.GetIO().Framerate,
                    ImGui.GetIO().Framerate));

                ImGui.InputText("Text input", _textBuffer, 100);

                ImGui.Text("Texture sample");
                ImGui.Image(_imGuiTexture, new Num.Vector2(300, 150), Num.Vector2.Zero, Num.Vector2.One, Num.Vector4.One,
                    Num.Vector4.One); // Here, the previously loaded texture is used
            }

            // 2. Show another simple window, this time using an explicit Begin/End pair
            if (show_another_window)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(200, 100), ImGuiCond.FirstUseEver);
                ImGui.Begin("Another Window", ref show_another_window);
                ImGui.Text("Hello");
                ImGui.End();
            }

            // 3. Show the ImGui test window. Most of the sample code is in ImGui.ShowTestWindow()
            if (show_test_window)
            {
                ImGui.SetNextWindowPos(new Num.Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref show_test_window);
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

        private void SetupDarkStyle()
        {
            ImGuiStylePtr style = ImGui.GetStyle();
            RangeAccessor<System.Numerics.Vector4> colors = style.Colors;

            //ImGuiCol_Text            
            //colors[0] = new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 1.000f));
            //ImGuiCol_TextDisabled
            //colors[1] = new System.Numerics.Vector4(0.500f, 0.500f, 0.500f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.TextDisabled, new System.Numerics.Vector4(0.500f, 0.500f, 0.500f, 1.000f));
            //ImGuiCol_WindowBg
            //colors[2] = new System.Numerics.Vector4(0.180f, 0.180f, 0.180f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0.180f, 0.180f, 0.180f, 1.000f));
            //ImGuiCol_ChildBg
            //colors[3] = new System.Numerics.Vector4(0.280f, 0.280f, 0.280f, 0.000f);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new System.Numerics.Vector4(0.280f, 0.280f, 0.280f, 0.000f));
            //ImGuiCol_PopupBg
            //colors[4] = new System.Numerics.Vector4(0.313f, 0.313f, 0.313f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new System.Numerics.Vector4(0.313f, 0.313f, 0.313f, 1.000f));
            //ImGuiCol_Border
            //colors[5] = new System.Numerics.Vector4(0.266f, 0.266f, 0.266f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.Border, new System.Numerics.Vector4(0.266f, 0.266f, 0.266f, 1.000f));
            //ImGuiCol_BorderShadow
            //colors[6] = new System.Numerics.Vector4(0.000f, 0.000f, 0.000f, 0.000f);
            ImGui.PushStyleColor(ImGuiCol.BorderShadow, new System.Numerics.Vector4(0.000f, 0.000f, 0.000f, 0.000f));
            //ImGuiCol_FrameBg
            //colors[7] = new System.Numerics.Vector4(0.160f, 0.160f, 0.160f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new System.Numerics.Vector4(0.160f, 0.160f, 0.160f, 1.000f));
            //ImGuiCol_FrameBgHovered
            //colors[8] = new System.Numerics.Vector4(0.200f, 0.200f, 0.200f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new System.Numerics.Vector4(0.200f, 0.200f, 0.200f, 1.000f));
            //ImGuiCol_FrameBgActive
            //colors[9] = new System.Numerics.Vector4(0.280f, 0.280f, 0.280f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new System.Numerics.Vector4(0.280f, 0.280f, 0.280f, 1.000f));
            //ImGuiCol_TitleBg
            //colors[10] = new System.Numerics.Vector4(0.148f, 0.148f, 0.148f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new System.Numerics.Vector4(0.148f, 0.148f, 0.148f, 1.000f));
            //ImGuiCol_TitleBgActive
            //colors[11] = new System.Numerics.Vector4(0.148f, 0.148f, 0.148f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new System.Numerics.Vector4(0.148f, 0.148f, 0.148f, 1.000f));
            //ImGuiCol_TitleBgCollapsed
            //colors[12] = new System.Numerics.Vector4(0.148f, 0.148f, 0.148f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, new System.Numerics.Vector4(0.148f, 0.148f, 0.148f, 1.000f));
            //ImGuiCol_MenuBarBg
            //colors[13] = new System.Numerics.Vector4(0.195f, 0.195f, 0.195f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new System.Numerics.Vector4(0.195f, 0.195f, 0.195f, 1.000f));
            //ImGuiCol_ScrollbarBg
            //colors[14] = new System.Numerics.Vector4(0.160f, 0.160f, 0.160f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new System.Numerics.Vector4(0.160f, 0.160f, 0.160f, 1.000f));
            //ImGuiCol_ScrollbarGrab
            //colors[15] = new System.Numerics.Vector4(0.277f, 0.277f, 0.277f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new System.Numerics.Vector4(0.277f, 0.277f, 0.277f, 1.000f));
            //ImGuiCol_ScrollbarGrabHovered
            //colors[16] = new System.Numerics.Vector4(0.300f, 0.300f, 0.300f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new System.Numerics.Vector4(0.300f, 0.300f, 0.300f, 1.000f));
            //ImGuiCol_ScrollbarGrabActive
            //colors[17] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f));
            //ImGuiCol_CheckMark
            //colors[18] = new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.CheckMark, new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 1.000f));
            //ImGuiCol_SliderGrab
            //colors[19] = new System.Numerics.Vector4(0.391f, 0.391f, 0.391f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.SliderGrab, new System.Numerics.Vector4(0.391f, 0.391f, 0.391f, 1.000f));
            //ImGuiCol_SliderGrabActive
            //colors[20] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f));
            //ImGuiCol_Button
            //colors[21] = new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.000f);
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.000f));
            //ImGuiCol_ButtonHovered
            //colors[22] = new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.156f);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.156f));
            //ImGuiCol_ButtonActive
            //colors[23] = new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.391f);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.391f));
            //ImGuiCol_Header
            //colors[24] = new System.Numerics.Vector4(0.313f, 0.313f, 0.313f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.Header, new System.Numerics.Vector4(0.313f, 0.313f, 0.313f, 1.000f));
            //ImGuiCol_HeaderHovered
            //colors[25] = new System.Numerics.Vector4(0.469f, 0.469f, 0.469f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new System.Numerics.Vector4(0.469f, 0.469f, 0.469f, 1.000f));
            //ImGuiCol_HeaderActive
            //colors[26] = new System.Numerics.Vector4(0.469f, 0.469f, 0.469f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new System.Numerics.Vector4(0.469f, 0.469f, 0.469f, 1.000f));
            //ImGuiCol_Separator
            //colors[27] = new System.Numerics.Vector4(0.266f, 0.266f, 0.266f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.Separator, new System.Numerics.Vector4(0.266f, 0.266f, 0.266f, 1.000f));
            //ImGuiCol_SeparatorHovered
            //colors[28] = new System.Numerics.Vector4(0.391f, 0.391f, 0.391f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.SeparatorHovered, new System.Numerics.Vector4(0.391f, 0.391f, 0.391f, 1.000f));
            //ImGuiCol_SeparatorActive
            //colors[29] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.SeparatorActive, new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f));
            //ImGuiCol_ResizeGrip
            //colors[30] = new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.250f);
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.250f));
            //ImGuiCol_ResizeGripHovered
            //colors[31] = new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.670f);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.670f));
            //ImGuiCol_ResizeGripActive
            //colors[32] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f));
            //ImGuiCol_Tab
            //colors[33] = new System.Numerics.Vector4(0.098f, 0.098f, 0.098f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.Tab, new System.Numerics.Vector4(0.098f, 0.098f, 0.098f, 1.000f));
            //ImGuiCol_TabHovered
            //colors[34] = new System.Numerics.Vector4(0.352f, 0.352f, 0.352f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.TabHovered, new System.Numerics.Vector4(0.352f, 0.352f, 0.352f, 1.000f));
            //ImGuiCol_TabActive
            //colors[35] = new System.Numerics.Vector4(0.195f, 0.195f, 0.195f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.TabActive, new System.Numerics.Vector4(0.195f, 0.195f, 0.195f, 1.000f));
            //ImGuiCol_TabUnfocused
            //colors[36] = new System.Numerics.Vector4(0.098f, 0.098f, 0.098f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocused, new System.Numerics.Vector4(0.098f, 0.098f, 0.098f, 1.000f));
            //ImGuiCol_TabUnfocusedActive
            //colors[37] = new System.Numerics.Vector4(0.195f, 0.195f, 0.195f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, new System.Numerics.Vector4(0.195f, 0.195f, 0.195f, 1.000f));

            //ImGuiCol_DockingPreview
            //colors[38] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 0.781f);

            //ImGuiCol_DockingEmptyBg
            //colors[39] = new System.Numerics.Vector4(0.180f, 0.180f, 0.180f, 1.000f);

            //ImGuiCol_PlotLines
            //colors[40] = new System.Numerics.Vector4(0.469f, 0.469f, 0.469f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.PlotLines, new System.Numerics.Vector4(0.469f, 0.469f, 0.469f, 1.000f));
            //ImGuiCol_PlotLinesHovered
            //colors[41] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.PlotLinesHovered, new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f));
            //ImGuiCol_PlotHistogram
            //colors[42] = new System.Numerics.Vector4(0.586f, 0.586f, 0.586f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new System.Numerics.Vector4(0.586f, 0.586f, 0.586f, 1.000f));
            //ImGuiCol_PlotHistogramHovered
            //colors[43] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogramHovered, new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f));
            //ImGuiCol_TextSelectedBg
            //colors[44] = new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.156f);
            ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new System.Numerics.Vector4(1.000f, 1.000f, 1.000f, 0.156f));
            //ImGuiCol_DragDropTarget
            //colors[45] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.DragDropTarget, new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f));
            //ImGuiCol_NavHighlight
            //colors[46] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.NavHighlight, new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f));
            //ImGuiCol_NavWindowingHighlight
            //colors[47] = new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f);
            ImGui.PushStyleColor(ImGuiCol.NavWindowingHighlight, new System.Numerics.Vector4(1.000f, 0.391f, 0.000f, 1.000f));
            //ImGuiCol_NavWindowingDimBg
            //colors[48] = new System.Numerics.Vector4(0.000f, 0.000f, 0.000f, 0.586f);
            ImGui.PushStyleColor(ImGuiCol.NavWindowingDimBg, new System.Numerics.Vector4(0.000f, 0.000f, 0.000f, 0.586f));
            //ImGuiCol_ModalWindowDimBg
            //colors[49] = new System.Numerics.Vector4(0.000f, 0.000f, 0.000f, 0.586f);
            ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, new System.Numerics.Vector4(0.000f, 0.000f, 0.000f, 0.586f));

            style.ChildRounding = 4.0f;
            style.FrameBorderSize = 1.0f;
            style.FrameRounding = 2.0f;
            style.GrabMinSize = 7.0f;
            style.PopupRounding = 2.0f;
            style.ScrollbarRounding = 12.0f;
            style.ScrollbarSize = 13.0f;
            style.TabBorderSize = 1.0f;
            style.TabRounding = 0.0f;
            style.WindowRounding = 4.0f;

        }
    }
}
