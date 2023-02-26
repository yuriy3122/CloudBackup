import {  UserRole, JobObjectType } from '../classes/Enums';
import * as moment from 'moment';

export class Formatters {
    static formatDateTimeAny(value: any) {
        var result = new Date(value.value.match(/\d+/)[0] * 1);
        return this.formatDateTime(result);
    }
    static formatDateTime(date: Date) {
        return moment(date).format('MMM DD, YYYY hh:mm:ss A');
    }

    static getJobObjectTypeShort(type: JobObjectType) {
        switch (type) {
            case JobObjectType.AuroraCluster:
                return "Aurora";
            case JobObjectType.RedshiftCluster:
                return "Redshift";
            case JobObjectType.Dynamo:
                return "Dynamo DB";
            default:
                return JobObjectType[type];
        }
    }
    static getJobObjectTypeShortPlural(type: JobObjectType) {
        switch (type) {
            case JobObjectType.Instance:
                return "Instances";
            case JobObjectType.Disk:
                return "Disks";
            default:
                return Formatters.getJobObjectTypeShort(type);
        }
    }
    /*
    static formatNAType(value: any) {
        return NetworkAnomalyType[value.value] || "Nothing";
    }*/

    static formatIPAddress(value: any) {
        var addr = value.value.m_Address;
        var res = "";
        for (var i = 0; i < 4; i++) {
            var num = addr & 255;
            res += num + ".";
            addr = addr >> 8;
        }
        res = res.slice(0, res.length - 1);
        return res;
    }

    static formatIPS(value: any) {
        if (value.value == null || Array.isArray(value.value) && value.value.length == 0)
            return "Any";

        return value.value.map(function (x: any) {
            if (x.addrType == 0) {
                return x.addr1; // IP
            } else if (x.addrType == 1) {
                return `${x.addr1}/${x.addr2}`;
            } else if (x.addrType == x.addr2) {
                return `${x.addr1}–${1}`;
            } else if (x.addrType == 3) {
                return `${x.addr1}/${x.Prefix}`;
            } else {
                return JSON.stringify(x);
            }
        }).join(", ");
    }
    static formatPorts(value: any) {
        let result = "";
        let ports = value.value;
        for (var i in ports) {
            let port = ports[i];
            if (port.minPort != port.maxPort)
                result += port.minPort + "-" + port.maxPort;
            else
                result += port.minPort.toString();

            result += ", ";
        }
        if (result.length > 0)
            result = result.slice(0, result.length - 2);

        if (result == "" || result == "0")
            result = "Any";
        return result;
    }
    static formatVms(value: any, vmsDict?: any, vmGroupsDict?: any): string {
        vmsDict = vmsDict || {};
        vmGroupsDict = vmGroupsDict || {};

        let that = this;
        if (value == null || value.value == null)
            return "Any";
        if (value.value.length == 0) {
            return "Any";
        } else if (value.value[0].toUpperCase() == 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF') {
            return "All";
        } else {
            return value.value.map(function (x: any) {
                x = x.toUpperCase();
                return vmsDict[x] || vmGroupsDict[x] || x; //add groups
            }).join(", ");
        }
    }
    static formatMAC(value: any) {
        return "Any";//value.value.join(', ');
    }

    static formatBool(value: any) {
        if (value.target == "row")
            return value.value ? "On" : "Off";
        return "";
    }

    static formatMBytes(value: any) {
        if (value.target == "row")
            return value.value + " MB";
        return "";
    }

    static formatPercents(value: any) {
        return value.value + "%";
    }

    static userTypeFormatter(value: any) {
        if (value.value) {
            return "Custom User";
        }

        return "Windows User";
    }

    static permissionsFormatter(value: any) {
        var permissions: Array<any> = [];
        if (value.value & UserRole.ITAdministrator) permissions.push("IT Administrator");
        if (value.value & UserRole.SecurityAdministrator) permissions.push("Security Administrator");
        if (value.value & UserRole.Auditor) permissions.push("Auditor");
        return permissions.join(", ");
    }
}
