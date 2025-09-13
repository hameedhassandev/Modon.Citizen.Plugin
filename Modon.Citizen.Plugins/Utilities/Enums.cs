using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modon.Citizen.Plugins
{
    public enum PortalStatus
    {
        Draft = 753240000,
        InProgress = 753240001,
        ActionRequired = 753240002,
        Done = 753240003,
        Rejected = 753240004,
        Resubmitted = 753240005
    }

    public enum CrmStatus
    {
        Draft = 753240000,
        AdminApproval = 753240001,
        ManagerApproval = 753240002,
        Done = 753240003,
        Rejected = 753240004,
        SendBackToCitizen = 753240005,
        InformationCompleted = 753240006,
    }

    public enum RequestType
    {
        New = 753240000,
        Renew = 753240001
    }

    public enum DocumentType
    {
        ID = 753240000,
        Passport = 753240001
    }

    public enum FirstApprovalStatus
    {
        Approved = 753240000,
        SentBack = 753240001,
        Rejected = 753240002
    }

    public enum SecondApprovalStatus
    {
        Approved = 753240000,
        SentBack = 753240001,
        Rejected = 753240002
    }

    public enum HeaderStatusCode
    {
        Draft = 1,
        Completed = 2,
        Cancelled = 753240000
    }

    public enum HeaderStateCode
    {
        Active = 0,
        Inactive = 1
    }

    public enum LogType
    {
        Info = 753240000,
        Warning = 753240001,
        Error = 753240002,
        Audit = 753240003,
        Action = 753240004
    }

    public enum LogVisibility
    {
        InternalCRM = 753240000,
        ExternalPortal = 753240001
    }

    public enum CitizenStatus
    {
        Draft = 1,
        Verified = 753240000,
        Suspended = 2
    }
}