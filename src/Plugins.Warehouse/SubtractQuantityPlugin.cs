using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using TALXIS.DevKit.Build.Dataverse.Plugin;

namespace Plugins.Warehouse
{
    /// <summary>
    /// Rule 2 (stock follows transactions): Inbound adds to the item's available quantity, Outbound subtracts.
    /// Runs in Post-operation, inside the database transaction, so the stock change and the
    /// transaction record commit or roll back together.
    /// </summary>
    [PluginStep("Create", "lab_warehousetransaction", PluginStage.PostOperation)]
    public class SubtractQuantityPlugin : PluginBase
    {
        private const int Inbound = 100000000;
        private const int Outbound = 100000001;

        public SubtractQuantityPlugin()
            : base(typeof(SubtractQuantityPlugin))
        {
        }

        public SubtractQuantityPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SubtractQuantityPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            var service = localPluginContext.OrgSvcFactory.CreateOrganizationService(context.UserId);

            if (!(context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity target) || target.LogicalName != "lab_warehousetransaction")
                return;

            if (!target.Contains("lab_quantity") || !target.Contains("lab_itemid") || !target.Contains("lab_transactiontype"))
                return;

            var quantity = target.GetAttributeValue<int>("lab_quantity");
            var itemRef = target.GetAttributeValue<EntityReference>("lab_itemid");
            var transactionType = target.GetAttributeValue<OptionSetValue>("lab_transactiontype");

            int delta;
            if (transactionType?.Value == Inbound)
                delta = quantity;
            else if (transactionType?.Value == Outbound)
                delta = -quantity;
            else
                return;

            var item = service.Retrieve("lab_warehouseitem", itemRef.Id, new ColumnSet("lab_availablequantity"));
            var available = item.GetAttributeValue<int?>("lab_availablequantity") ?? 0;

            service.Update(new Entity("lab_warehouseitem", itemRef.Id)
            {
                ["lab_availablequantity"] = available + delta
            });
        }
    }
}
