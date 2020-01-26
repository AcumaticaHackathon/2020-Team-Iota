using System;
using System.Collections;
using System.Collections.Generic;
using PX.SM;
using PX.Data;
using System.Linq;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.CR;
using PX.Objects.IN;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LuisAutoMailer
{
    public class MailerSOGraph : PXGraph<MailerSOGraph>
    {
        public void GenerateSalesOrders(string JsonPackage, string mailFrom, DateTime orderDate)
        {
            try
            {
                SOOrderEntry soGraph = PXGraph.CreateInstance<SOOrderEntry>();
                soGraph.Clear();

                PXTrace.WriteInformation("SO Order Graph instantiated.");

                #region Variables

                JObject package = JObject.Parse(JsonPackage);
                
                int countLines = 0;
                decimal qty = 0.0M;

                //Customer
                Customer customer = PXSelect<Customer,
                                Where<Customer.acctCD, Equal<Customer.acctCD>>>.
                                Select(soGraph, "ABCHOLDING");


                //Customer Default Location
                Location arCustomerLocation = PXSelect<Location,
                                              Where<Location.isActive, Equal<Required<Location.isActive>>,
                                                  And<Location.bAccountID, Equal<Required<Location.bAccountID>>,
                                                  And<Location.locationCD, Equal<Required<Location.locationCD>>>>>>.
                                              Select(soGraph, true, customer.BAccountID, "MAIN");
                
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
                    soGraph.Document.Cache.SetValueExt<SOOrder.customerOrderNbr>(order, "");
                    soGraph.Document.Cache.SetValueExt<SOOrder.orderDate>(order, DateTime.Now);
                    soGraph.Document.Cache.SetValueExt<SOOrder.requestDate>(order, orderDate);

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

                    //Details

                    SuperJSon superJSon = JsonConvert.DeserializeObject<SuperJSon>(package.ToString());
                    List<string> inventorydesc = new List<string>();
                    int Myqty = 0;
                    foreach (var item in superJSon.compositeEntities)
                    {
                        if (item.children[0].type == "product")
                        {
                            inventorydesc.Add(item.children[0].value);
                        }
                        else
                        {
                            Myqty = Convert.ToInt32(item.children[0].value);
                        }
                    }

                    //    JObject saleLine = JObject.Parse(sl);

                    var inItem = new PXSelect<InventoryItem>(this).Select().AsQueryable();

                    foreach (string item in inventorydesc)
                    {
                        inItem = inItem.Where(i => i.GetItem<InventoryItem>().Descr.Contains(item));
                    }

                    InventoryItem ResultInventory = inItem.First();


                    INSite warehouse = PXSelectReadonly<INSite,
                                          Where<INSite.siteCD, Equal<Required<INSite.siteCD>>>>.
                                          Select(soGraph, "WHOLESALE");


                    if (inItem != null)
                    {
                        try
                        {
                            SOLine orderLine = soGraph.Transactions.Insert();

                            soGraph.Transactions.Cache.SetValueExt<SOLine.inventoryID>(orderLine, ResultInventory.InventoryID);
                            soGraph.Transactions.Cache.SetValueExt<SOLine.siteID>(orderLine, warehouse.SiteID);

                            qty = Convert.ToDecimal(Myqty);

                            soGraph.Transactions.Cache.SetValueExt<SOLine.orderQty>(orderLine, qty);

                            orderLine = soGraph.Transactions.Update(orderLine);

                            countLines++;
                        }
                        catch (Exception)
                        {
                            hasError = true;  //Failed
                        }
                    }
                    else
                    {
                        hasError = true;  //Failed
                    }
                }

                PXTrace.WriteInformation("SO Order Details added. ");

                if (hasError == false)
                {
                   //ADD EMAIL


                    order = soGraph.Document.Update(order);
                    try
                    {
                        soGraph.Actions.PressSave();
                        PXTrace.WriteInformation("Order [" + soGraph.Document.Current.RefNbr + "] created ");
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
            catch (Exception e)
            {
                PXTrace.WriteInformation(e.Message);
            }
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