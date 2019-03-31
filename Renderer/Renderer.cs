using MgCoreEditor.Engine;
using MgCoreEditor.Settings;
using Microsoft.Xna.Framework;

namespace MgCoreEditor.Renderer
{
    public class Renderer
    {
        //PerspectiveCamera
        private IEditorCamera _perspectiveCamera;

        //Projection Matrices and derivates used in shaders
        private Matrix _view;
        private Matrix _inverseView;
        private Matrix _viewIT;
        private Matrix _projection;
        private Matrix _viewProjection;
        private Matrix _staticViewProjection;
        private Matrix _inverseViewProjection;
        private Matrix _previousViewProjection;
        private Matrix _currentViewToPreviousViewProjection;

        //View Projection
        private bool _viewProjectionHasChanged;
        private Vector3 _inverseResolution;

        public Renderer(IEditorCamera perspectiveCamera)
        {
            _perspectiveCamera = perspectiveCamera;
        }

        public void Update(GameTime gameTime)
        {
            UpdateViewProjection();
        }

        public Matrix View
        {
            get { return _view; }
        }

        public Matrix Projection
        {
            get { return _projection; }
        }

        public Matrix ViewProjection
        {
            get { return _viewProjection; }
        }

        //Update our view projection matrices if the perspectiveCamera moved
        /// <summary>
        /// Create the projection matrices
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="meshMaterialLibrary"></param>
        /// <param name="entities"></param>
        private void UpdateViewProjection()
        {
            _viewProjectionHasChanged = _perspectiveCamera.Transform.HasChanged;


            //If the perspectiveCamera didn't do anything we don't need to update this stuff
            if (_viewProjectionHasChanged)
            {
                //We have processed the change, now setup for next frame as false
                _perspectiveCamera.Transform.HasChanged = false;
                _perspectiveCamera.Transform.HasMoved = false;

                //View matrix
                _view = Matrix.CreateLookAt(_perspectiveCamera.Transform.Position, _perspectiveCamera.Lookat, _perspectiveCamera.Up);
                _inverseView = Matrix.Invert(_view);
                _viewIT = Matrix.Transpose(_inverseView);


                _projection = Matrix.CreatePerspectiveFieldOfView(_perspectiveCamera.FieldOfView,
                    GameSettings.g_screenwidth / (float)GameSettings.g_screenheight, GameSettings.g_nearPlane, GameSettings.g_farplane);

                //_gBufferRenderModule.PerspectiveCamera = _perspectiveCamera.Position;

                _viewProjection = _view * _projection;

                //this is the unjittered viewProjection. For some effects we don't want the jittered one
                _staticViewProjection = _viewProjection;

                //Transformation for TAA - from current view back to the old view projection
                _currentViewToPreviousViewProjection = Matrix.Invert(_view) * _previousViewProjection;


                _previousViewProjection = _viewProjection;
                _inverseViewProjection = Matrix.Invert(_viewProjection);
            }
        }
    }
}