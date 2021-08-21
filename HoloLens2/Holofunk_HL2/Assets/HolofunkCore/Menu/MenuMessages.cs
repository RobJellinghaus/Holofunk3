// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;

namespace Holofunk.Menu
{
    public class MenuMessages : Messages
    {
        public class Create : CreateMessage
        {
            public MenuState MenuState { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, MenuState Menu) : base(id) { MenuState = Menu; }
            public override string ToString() => $"{base.ToString()}{MenuState}";
        }

        public class Delete : DeleteMessage
        {
            public Delete() : base() { }
            public Delete(DistributedId id, bool isRequest) : base(id, isRequest) { }
            public override string ToString() => $"{base.ToString()}{Id}";
        }

        public class SetSelected : ReliableMessage
        {
            public MenuItemId TopSelectedItem { get; set; }
            public MenuItemId SubSelectedItem { get; set; }
            public SetSelected() : base() { }
            public SetSelected(DistributedId id, bool isRequest, MenuItemId topSelectedItem, MenuItemId subSelectedItem)
                : base(id, isRequest)
            {
                TopSelectedItem = topSelectedItem;
                SubSelectedItem = subSelectedItem;
            }
            public override void Invoke(IDistributedInterface target) => ((IDistributedMenu)target).SetSelection(TopSelectedItem, SubSelectedItem);
        }

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType(MenuItemId.Serialize, MenuItemId.Deserialize);
            proxyCapability.RegisterType(MenuKind.Serialize, MenuKind.Deserialize);
            proxyCapability.RegisterType<MenuState>();
        }

        // TODO: refactor this for actual sharing with the other Register methods
        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedMenu, LocalMenu, IDistributedMenu>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Menu,
                (local, message) => local.Initialize(message.MenuState));
            Registrar.RegisterDeleteMessage<Delete, DistributedMenu, LocalMenu, IDistributedMenu>(proxyCapability);
            Registrar.RegisterReliableMessage<SetSelected, DistributedMenu, LocalMenu, IDistributedMenu>(proxyCapability);
        }
    }
}
