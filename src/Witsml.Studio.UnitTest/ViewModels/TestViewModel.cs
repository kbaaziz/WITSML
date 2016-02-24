﻿using Caliburn.Micro;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// An IPluginViewModel for testing
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    /// <seealso cref="PDS.Witsml.Studio.ViewModels.IPluginViewModel" />
    public class TestViewModel : Screen, IPluginViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestViewModel"/> class.
        /// </summary>
        public TestViewModel()
        {
            DisplayName = DisplayOrder.ToString();
        }

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder
        {
            get
            {
                return 100;
            }
        }
    }
}