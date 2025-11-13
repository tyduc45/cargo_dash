using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.script
{
    public interface IInteractable
    {
        void Interact(controller player, bool isHold);

        string GetInteractPrompt();
    }
}
