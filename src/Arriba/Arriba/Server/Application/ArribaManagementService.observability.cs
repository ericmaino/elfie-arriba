using Arriba.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arriba.Communication.Server.Application
{

 

    public partial class ArribaManagementService
    {
        public event EventHandler<TableContextEventArgs> CreatedUserTable;
        public event EventHandler<TableContextEventArgs> SavedUserTable;

        protected virtual void OnRaiseCreatedUserTable(TableContextEventArgs e)
        {
            EventHandler<TableContextEventArgs> raiseEvent = CreatedUserTable;

            if (raiseEvent != null)
            {
                raiseEvent(this, e);
            }
        }
        protected virtual void OnRaiseDeletedUserTable(TableContextEventArgs e)
        {
            EventHandler<TableContextEventArgs> raiseEvent = SavedUserTable;

            if (raiseEvent != null)
            {
                raiseEvent(this, e);
            }
        }


        public class TableContextEventArgs : EventArgs
        {
            private readonly Table table;

            public TableContextEventArgs(Table table)
            {
                this.table = table;
            }
            public Table Table => table;
        }
    }
}
