using UnityEngine;
using UnityEngine.XR;

namespace UnityARInterface
{
    /// <summary>
    /// AR Interface background renderer.
    /// </summary>
    public class ARInterfaceBackgroundRenderer : ARBackgroundRenderer
    {
        public bool Enabled { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:UnityARInterface.ARInterfaceBackgroundRenderer"/> class.
        /// </summary>
        public ARInterfaceBackgroundRenderer()
        {
            Enabled = true;
        }

        /// <summary>
        /// Enables the rendering.
        /// </summary>
        /// <returns><c>true</c>, if rendering was enabled, <c>false</c> otherwise.</returns>
        public bool EnableRendering()
        {
            return Enabled = EnableARBackgroundRendering();
        }

        /// <summary>
        /// Disables the rendering and sets camera to draw black solid color
        /// </summary>
        public void DisableRendering()
        {
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = Color.black;

            DisableARBackgroundRendering();
        
            Enabled = false;
        }
    }
}