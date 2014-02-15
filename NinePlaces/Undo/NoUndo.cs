using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common;

namespace NinePlaces.Undo
{
    /// <summary>
    /// NoUndo  basically  just  deliberately ignores undo events.
    /// </summary>
    public class NoUndo : BaseUndoWrapper
    {
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
