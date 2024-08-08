export default class EnvHelper {
    public static isDevelopment = () => process.env.REACT_APP_Environment?.toLowerCase() === "development";
    public static isProduction = () => process.env.REACT_APP_Environment?.toLowerCase() === "production";
    public static isDebugMode = () => process.env.REACT_APP_DebugMode?.toLowerCase() === "true";
    public static usePredefinedHost = () => process.env.REACT_APP_Use_Predefined_Host?.toLowerCase() === "true";
}