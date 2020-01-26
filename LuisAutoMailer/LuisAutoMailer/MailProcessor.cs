using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.SM;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.SM;
using PX.Objects.EP;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

namespace LuisAutoMailer
{
    public class AnotherServiceRegistration : Autofac.Module
    {
        protected override void Load(Autofac.ContainerBuilder builder)
        {
            PX.Data.DependencyInjection.ApplicationStartActivation
                .ActivateOnApplicationStart<MailProcessor>(builder, PX.Objects.EP.EmailProcessorManager.Register);
        }
    }
       

    public class MailProcessor : BasicEmailProcessor
    {       
        public MailProcessor()
        {
            PXTrace.WriteInformation("Sales Order mail Processor instantiated. ");
        }

        protected override bool Process(Package package)
        {
            PXTrace.WriteInformation("Processing email package. ");

            var account = package.Account;

            var accountExt = account.GetExtension<SMEMailAccountExt>();

            //Checkin if Incoming Processing is active & SO Processing
            if (account.IncomingProcessing != true ||
                accountExt.UsrSOProcessing != true)
            {
                PXTrace.WriteInformation("IncomingProcessing: " + account.IncomingProcessing + ", SOProcessing: " + accountExt.UsrSOProcessing + ". ");
                return false;
            }

            var message = package.Message;

            //Check if not empty email
            if (!string.IsNullOrEmpty(message.Exception)
                || message.IsIncome != true
                || message.RefNoteID != null)
            {

                PXTrace.WriteInformation("IsIncome: " + message.IsIncome + ", RefNoteID: " + message.RefNoteID + ". ");
                return false;
            }

            if (message.Subject.Contains("Massbuild") && message.Subject.Contains("Order Number"))
            {

                PXTrace.WriteInformation("Start Processing of email: " + message.Subject + "");

                //Create BuildersPO Graph
                BuildersPOSOEntry buildersGraph = PXGraph.CreateInstance<BuildersPOSOEntry>();

                //Get File Attachment
                UploadFile file = SelectFrom<UploadFile>.
                                  InnerJoin<NoteDoc>.On<UploadFile.fileID.IsEqual<NoteDoc.fileID>>.SingleTableOnly.
                                  Where<NoteDoc.noteID.IsEqual<@P.AsGuid>
                                        .And<UploadFile.name.IsLike<@P.AsString>>>.View.Select(package.Graph, message.NoteID, "%.csv%");

                if (file != null)
                {
                    var fm = PXGraph.CreateInstance<UploadFileMaintenance>();
                    PX.SM.FileInfo attachment = fm.GetFile(new Guid(file.FileID.ToString()));

                    PXTrace.WriteInformation("Calling SO Import function");

                    buildersGraph.ImportStatement(attachment, message.Subject, false);
                    return true;
                }
                else
                {
                    PXTrace.WriteInformation("Message Attachment could not be retrieved: " + message.Subject + "");
                    return false;
                }
            }
            else
            {
                PXTrace.WriteInformation("Message Subject validation could not pick up keys: " + message.Subject + "");
                return false;
            }
        }
    }
}