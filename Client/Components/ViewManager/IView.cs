﻿using Microsoft.AspNetCore.Components;
using System;

namespace CEC.Blazor.Examples.Components
{
    public interface IView : IComponent
    {
        public Guid GUID => Guid.NewGuid();

        [CascadingParameter] public ViewManager ViewManager { get; set; }

    }
}
