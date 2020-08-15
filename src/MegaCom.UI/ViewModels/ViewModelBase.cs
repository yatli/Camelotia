using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MegaCom.UI
{
    // Top-level tab vm base class
    public abstract class ViewModelBase : ReactiveObject
    {
        public abstract string Name { get; }
    }
}