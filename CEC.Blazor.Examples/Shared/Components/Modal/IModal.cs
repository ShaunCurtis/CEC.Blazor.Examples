﻿using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace CEC.Blazor.Examples.Client.Components
{
    public interface IModal
    {
        ModalOptions Options { get; set; }

        Task<ModalResult> ShowAsync<TModal>(ModalOptions options) where TModal : IComponent;

        void Update(ModalOptions options = null);

        void Dismiss();

        void Close(ModalResult result);
    }
}
