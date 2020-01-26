using PX.Data;
using PX.Objects;
using PX.SM;
using System;

namespace PX.SM
{
    public class EMailAccountExt : PXCacheExtension<PX.SM.EMailAccount>
    {
        #region UsrCustomMailer
        [PXDBBool]
        [PXUIField(DisplayName = "CustomMailer")]

        public virtual bool? UsrCustomMailer { get; set; }
        public abstract class usrCustomMailer : PX.Data.BQL.BqlBool.Field<usrCustomMailer> { }
        #endregion
    }
}