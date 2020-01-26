using System;
using System.Collections;
using System.Collections.Generic;
using PX.SM;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.CR;
using PX.Objects.IN;

namespace LuisAutoMailer
{
    public class MailerSOGraph : PXGraph<MailerSOGraph>
    {
        private void GenerateSalesOrders(object JsonPackage)
        {
            try
            {
                SOOrderEntry soGraph = PXGraph.CreateInstance<SOOrderEntry>();
                soGraph.Clear();

                PXTrace.WriteInformation("SO Order Graph instantiated.");

                #region Variables

                int countLines = 0;

                //Customer
                BAccount customer = PXSelect<BAccount,
                                Where<BAccount.acctCD, Equal<BAccount.acctCD>>>>.
                                Select(soGraph, graphOrderEntry.Order.Current.SiteCode);

                //Customer Default Location
                Location arCustomerLocation = PXSelect<Location,
                                              Where<Location.isActive, Equal<Required<Location.isActive>>,
                                                  And<Location.bAccountID, Equal<Required<Location.bAccountID>>,
                                                  And<Location.locationCD, Equal<Required<Location.locationCD>>>>>>.
                                              Select(soGraph, true, customer.BAccountID, "MAIN");

                //Warehouse
                INSite warehouse = PXSelect<INSite,
                                   Where<INSite.siteCD, Equal<Required<INSite.siteCD>>>>.
                                   Select(soGraph, PnPSetup.Current.Warehouse); //[DIST-TRU-CT]

                PXTrace.WriteInformation("SO Order Graph variables set.");
                #endregion

                //Header
                bool hasError = false;
                string ms = "";
                SOOrder order = new SOOrder();
                order.OrderType = "QT";

                try
                {
                    order = soGraph.Document.Insert(order);

                    soGraph.Document.Cache.SetValueExt<SOOrder.customerID>(order, customer.BAccountID);
                    soGraph.Document.Cache.SetValueExt<SOOrder.customerLocationID>(order, arCustomerLocation.LocationID);
                    soGraph.Document.Cache.SetValueExt<SOOrder.customerOrderNbr>(order, graphOrderEntry.Order.Current.PONumber);
                    soGraph.Document.Cache.SetValueExt<SOOrder.orderDate>(order, DateTime.Now);
                    soGraph.Document.Cache.SetValueExt<SOOrder.requestDate>(order, graphOrderEntry.Order.Current.DeliveryDate);

                    //Update Order
                    order = soGraph.Document.Update(order);

                }
                catch (Exception er)
                {
                    hasError = true;
                    ms = er.Message;
                }

                if (hasError == false)
                {
                    PXTrace.WriteInformation("SO Order Header added. Starting rows. ");
                    decimal ediTotal = 0M;

                    //Details
                    foreach (PPOrderLine ediOrderLine in graphOrderEntry.OrderLine.Select())
                    {
                        decimal qty = 0M;

                        //Get Inventory ID
                        InventoryItem inItem = PXSelectReadonly2<InventoryItem,
                                                InnerJoin<INItemXRef, On<InventoryItem.inventoryID, Equal<INItemXRef.inventoryID>>,
                                                InnerJoin<BAccount, On<BAccount.bAccountID, Equal<INItemXRef.bAccountID>>>>,
                                                Where<INItemXRef.alternateID, Equal<Required<INItemXRef.alternateID>>>>.
                                                Select(soGraph, ediOrderLine.ArticleNumber, PnPSetup.Current.PnPCustomerID);

                        if (inItem != null)
                        {
                            try
                            {
                                SOLine orderLine = soGraph.Transactions.Insert();

                                soGraph.Transactions.Cache.SetValueExt<SOLine.inventoryID>(orderLine, inItem.InventoryID);
                                soGraph.Transactions.Cache.SetValueExt<SOLine.siteID>(orderLine, warehouse.SiteID);

                                if (inItem.SalesUnit == "EACH")
                                {
                                    qty = Convert.ToDecimal(ediOrderLine.RequestedQuantity) * Convert.ToDecimal(ediOrderLine.PackSize);
                                }
                                else
                                {
                                    qty = Convert.ToDecimal(ediOrderLine.RequestedQuantity);
                                }
                                soGraph.Transactions.Cache.SetValueExt<SOLine.orderQty>(orderLine, qty);

                                //Update Item
                                soGraph.Transactions.Cache.SetValueExt<SOLineExt.usrpnpPrice>(orderLine, ediOrderLine.NetPrice / 1.15M);
                                orderLine = soGraph.Transactions.Update(orderLine);

                                ediTotal = ediTotal + Convert.ToDecimal(ediOrderLine.NetPrice) / 1.15M;

                                ediOrderLine.TrxStatus = "P";                                                           //Processed
                                countLines++;
                            }
                            catch (Exception er)
                            {
                                if (er.Message.Length >= 100)
                                {
                                    ediOrderLine.TrxMessage = er.Message.Substring(0, 100);
                                }
                                else
                                {
                                    ediOrderLine.TrxMessage = er.Message;
                                }

                                ediOrderLine.TrxStatus = "F";                                                           //Failed
                                hasError = true;
                            }
                        }
                        else
                        {
                            PXTrace.WriteInformation("SO Order detail, " +
                                                    ediOrderLine.ArticleNumber +
                                                    " could not be found in inventory. ");

                            log.TrxLog += "ERROR ***Item " + ediOrderLine.ArticleNumber + " does not exist ***";
                            ediOrderLine.TrxMessage = "Item(s) does not exist.";
                            ediOrderLine.TrxStatus = "F";                                                               //Failed
                        }

                        //Update EDI Orderline 
                        this.OrderLine.Update(ediOrderLine);
                    }

                    PXTrace.WriteInformation("SO Order Details added. ");

                    if (hasError == false)
                    {
                        //Sales Order
                        soGraph.Document.Cache.SetValueExt<SOOrderExt.usrEDITotal>(order, ediTotal);

                        order = soGraph.Document.Update(order);
                        try
                        {
                            soGraph.Actions.PressSave();

                            log.TrxLog += "Order [" + soGraph.Document.Current.RefNbr + "] created. ";
                            PXTrace.WriteInformation("Updated & Saved SO Order. ");
                        }
                        catch (PXException pEx)
                        {
                            PXTrace.WriteInformation(pEx.Message + " " + pEx.InnerException);
                        }
                    }
                    else
                    {
                        PXTrace.WriteInformation("Errors on order, could not save. ");
                    }
                }
                else
                {
                    PXTrace.WriteInformation(ms);
                }
            }
            catch (Exception e)
            {
                PXTrace.WriteInformation(e.Message);
            }
            return log;
        }

        //Internal View
        internal class DummyView : PXView
        {
            List<object> _Records;
            internal DummyView(PXGraph graph, BqlCommand command, List<object> records)
                : base(graph, true, command)
            {
                _Records = records;
            }
            public override List<object> Select(object[] currents, object[] parameters, object[] searches, string[] sortcolumns, bool[] descendings, PXFilterRow[] filters, ref int startRow, int maximumRows, ref int totalRows)
            {
                return _Records;
            }
        }
    }

}