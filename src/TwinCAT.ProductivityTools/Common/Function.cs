using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace TwinCAT.Remote
{
    public class Function
    {
        public string Name { get; set; }
        public FunctionType Type { get; set; }
        public Version Version { get; set; }

        public static async Task<IEnumerable<Function>> ListFunctionsAsync(AmsNetId target, CancellationToken cancel)
        {
            var functions = new List<Function>();

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));

                foreach( var key in RegistryDefinitions.Keys)
                {
                    try
                    {
                        var name = RegistryDefinitions[key];
                        var regKey = @"SOFTWARE\WOW6432Node\Beckhoff\TwinCAT3 Functions\" + name + @"\Common";

                        var version = await Registry.QueryValueAsync(target, regKey, "Version");

                        functions.Add(new Function
                        {
                            Name = name,
                            Version = System.Version.Parse(version),
                            Type = key
                        });

                    } 
                    catch { }
                }
            }

            return functions;
        }

        private static Dictionary<FunctionType, string> RegistryDefinitions = new Dictionary<FunctionType, string>
        {
            {FunctionType.TargetBrowser, "Beckhoff Support Info Report"},
            {FunctionType.SupportInfoReport, "Beckhoff Target Browser"},
            {FunctionType.BlockDiagram, "Beckhoff TwinCAT 3 Block Diagram"},
            {FunctionType.TE1010_RealtimeMonitor, "Beckhoff TE1010 Realtime Monitor"},
            {FunctionType.TE1120_XCADInterface, "Beckhoff TE1120 TC3 XCAD Interface"},
            {FunctionType.TE1300_ScopeView, "TE130X Scope View"},
            {FunctionType.TE1300_ScopeViewProfessional, "Beckhoff TE130x Scope View"},     
            //{TcFunctionType.TE1310_FilterDesigner, ""},
            {FunctionType.TE1320_BodePlot, "Beckhoff TE132x Bode Plot"},
            {FunctionType.TE1400_TargetForMatlab, "TE1400 TargetForMatlabSimulink"},
            {FunctionType.TE1410_InterfaceForMatlab, "TE1410-InterfaceForMatlabSimulink"},
            {FunctionType.TE1610_EAPConfigurator, "TE1610 TC3 EAP-Configurator"},
            {FunctionType.TE2000_TwinCATHMI, "TE2000 HMI Engineering"},
            //{TcFunctionType.TE3520_AnalyticsServiceTool, ""},
            {FunctionType.TE5910_MotionDesigner, "TE5910-Motion-Designer"},
            {FunctionType.TE5950_DriveManager2, "TF5950 Drive Manager 2"}, 
            //{TcFunctionType.TF1810_PlcHmiWeb, ""},
            {FunctionType.TF2000_TwinCATHMIServer, "TF2000 HMI Server"},
            {FunctionType.TF3110_FilterDesigner, "Beckhoff TF3110 TC3 Filter Designer"},
            {FunctionType.TF3300_ScopeServer, "TF3300 Scope Server"},
            {FunctionType.TF3520_AnalyticsStorageProvider, "TF3520 Analytics StorageProvider"},
            {FunctionType.TF3600_ConditionMonitoringLevel1, "TF36xx Condition Monitoring"},
            {FunctionType.TF3650_PowerMonitoring, "Beckhoff TF3650 Power Monitoring"},
            //{TcFunctionType.TF3680_Filter, ""},
            {FunctionType.TF3900_SolarPositionAlgorithm, "TF3900 Solar Position Algorithm"},
            {FunctionType.TF4100_ControllerToolbox, "TF4100 Controller Toolbox"},
            {FunctionType.TF4110_TemperatureController, "TF4110 Temperature Controller"},
            {FunctionType.TF5110_KinematicTransformation, ""},
            {FunctionType.TF5270_CncVirtualNCK, "Beckhoff TF527x CNC-Virtual NCK"},
            {FunctionType.TF5400_AdvancedMotionPack, "Beckhoff TF5400 TC3 Advanced Motion Pack"},
            //{TcFunctionType.TF5420_MotionPickAndPlace, ""},
            {FunctionType.TF5810_HydraulicPositioning, "TF5810-TC3-Hydraulic-Positioning"},
            {FunctionType.TF5850_XtsExtension, "TF5850 TC3 XTS Technology"},
            {FunctionType.TF6010_AdsMonitor, "TF6010 ADS Monitor"},
            {FunctionType.TF6100_OpcUa, "TF6100 TC3 OPC-UA"},
            {FunctionType.TF6120_OpcDa, "TF6120 TC3 OPC-DA"},
            {FunctionType.TF6250_ModbusTcp, "TF6250 Modbus TCP"},
            {FunctionType.TF6300_FtpClient, "TF6300 FTP"},
            {FunctionType.TF6310_TcpIp, "TF6310 TC3 TCP/IP"},
            {FunctionType.TF6340_SerialCommunication, "TF6340 Serial Communication"},
            {FunctionType.TF6350_SmsSmtp, "TF6350 SMS SMTP"},
            {FunctionType.TF6360_VirtualSerialCom, "TF6360 Virtual Serial COM"},
            {FunctionType.TF6420_DatabaseServer, "TF6420 Database Server"},
            {FunctionType.TF6421_XMLServer, "TF6421 XML Server"},
            {FunctionType.TF6500_IEC60870_5_10x, "TF6500 IEC 60870-5-10x"},
            {FunctionType.TF6600_RFIDReaderCommunication, "TF6600 RFID Reader Communication"},
            {FunctionType.TF6720_IoTDataAgent, "TF6720 IoT Data Agent"},
            //{TcFunctionType.TF8000_HVAC, ""},
            {FunctionType.TF8010_BuildingAutomationBasic, "Beckhoff TF8010 Building Automation Basic"},
            {FunctionType.TF8040_BuildingAutomation, "Beckhoff TF8040 Building Automation"},
            {FunctionType.TF8310_WindFramework, "TF8310 Wind Framework"},
            {FunctionType.TF8810_AES70_OCA, "TF8810 AES70 (OCA)"},
        };
    }


    public enum FunctionType
    {
        TE1010_RealtimeMonitor,
        TE1120_XCADInterface,
        TE1300_ScopeViewProfessional,
        TE1310_FilterDesigner,
        TE1400_TargetForMatlab,
        TE1410_InterfaceForMatlab,
        TE1610_EAPConfigurator,
        TE2000_TwinCATHMI,
        TE3520_AnalyticsServiceTool,
        TE5910_MotionDesigner,
        TE5950_DriveManager2,
        TF1810_PlcHmiWeb,
        TF2000_TwinCATHMIServer,
        TF3520_AnalyticsStorageProvider,
        TF3600_ConditionMonitoringLevel1,
        TF3650_PowerMonitoring,
        TF3680_Filter,
        TF3900_SolarPositionAlgorithm,
        TF4100_ControllerToolbox,
        TF4110_TemperatureController,
        TF5110_KinematicTransformation,
        TF5270_CncVirtualNCK,
        TF5410_MotionCollisionAvoidance,
        TF5420_MotionPickAndPlace,
        TF5810_HydraulicPositioning,
        TF5850_XtsExtension,
        TF6100_OpcUa,
        TF6120_OpcDa,
        TF6250_ModbusTcp,
        TF6300_FtpClient,
        TF6310_TcpIp,
        TF6340_SerialCommunication,
        TF6350_SmsSmtp,
        TF6360_VirtualSerialCom,
        TF6420_DatabaseServer,
        TF6421_XMLServer,
        TF6500_IEC60870_5_10x,
        TF6600_RFIDReaderCommunication,
        TF6720_IoTDataAgent,
        TF8000_HVAC,
        TF8010_BuildingAutomationBasic,
        TF8040_BuildingAutomation,
        TF8310_WindFramework,
        TF8810_AES70_OCA,
        TargetBrowser,
        SupportInfoReport,
        BlockDiagram,
        TE1300_ScopeView,
        TE1320_BodePlot,
        TF3300_ScopeServer,
        TF5400_AdvancedMotionPack,
        TF6010_AdsMonitor,
        TF3110_FilterDesigner,
    }
}
