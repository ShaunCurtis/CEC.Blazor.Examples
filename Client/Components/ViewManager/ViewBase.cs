using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CEC.Blazor.Examples.Components
{
    public class ViewBase : Component, IView
    {
        [CascadingParameter]
        public ViewManager ViewManager { get; set; }
    }
}
