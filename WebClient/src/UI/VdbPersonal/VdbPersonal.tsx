import cl from "./VdbPersonal.module.css";
import { NavLink, useNavigate } from 'react-router-dom';
import GlobalContext from '../../helpers/GlobalContext';
import hrefs from "../../config/hrefsList.json";
import { useState, useEffect, useMemo } from 'react';
import { CSSTransition } from 'react-transition-group';
import DevicesList from "../DevicesList/DevicesList";
import AuthHelper from '../../helpers/AuthHelper';
import UserApiHelper from '../../helpers/UserApiHelper';
import ISessionsResponse from '../../models/Auth/ISessionsResponse';
import VdbSecurity from '../VdbSecurity/VdbSecurity';
import EnvHelper from '../../helpers/EnvHelper';
import IUserInfoFromJwt from '../../models/Auth/IUserInfoFromJwt';
import ApiHelper from '../../helpers/ApiHelper';

const VdbPersonal: React.FC = () => {
    const [transState, setTransState] = useState(false);
    const [user, setUser] = useState<string | undefined>(undefined);
    const [limit, setLimit] = useState(0);
    const navigate = useNavigate();

    useMemo(async () => {
        try {
            await AuthHelper.EnsureUserInContext();
        } catch {
            console.info("Redirecting to auth because error occured during user loading.");
            navigate("/auth");
        } finally {
            if (GlobalContext.currentUser === undefined && (!EnvHelper.isDebugMode() || EnvHelper.isProduction())) {
                console.info("Redirecting to auth because no user available.");
                navigate("/auth");
                return;
            }
        }

        ApiHelper.getDevicesLimits().then(r => {
            console.info(`Current access level: ${GlobalContext.GetAccessLevel()}`);
            var limit = r?.filter(x => x.accessLevel === GlobalContext.GetAccessLevel())[0].devicesLimit;
            if (limit !== undefined) setLimit(limit);
        });

        setUser(GlobalContext.currentUser!.Email);

        setTransState(true)
    }, []);

    const logout = async () => {
        console.log("Log out required...");
        await new UserApiHelper().terminateThisSession();
        GlobalContext.logout();
        navigate("/");
        window.location.reload();
    }


    const transitionClasses = {
        enterActive: cl.welcomeTransitionEnter,
        enterDone: cl.welcomeTransitionEnterActive,
        exitActive: cl.welcomeTransitionExit,
        exitDone: cl.welcomeTransitionExitActive,
    }
    const commonTransProp = {
        timeout: 300,
        classNames: transitionClasses
    }

    return (
        <CSSTransition in={transState} {...commonTransProp}>
            <span className={cl.personalWrapper}>
                <span className={cl.loggedAs}>
                    You are logged in as: <span className={cl.loggedAsEmail}>
                        {user ?? "unknown"}
                    </span>
                    <button className={cl.logoutBtn} onClick={logout}>
                        LOG OUT
                    </button>
                </span>
                <span className={cl.loggedAs}>Devices limit: <span style={{ fontWeight: "500" }}>{limit}</span></span>

                <span className={cl.personalText}>
                    Security:
                </span>
                <VdbSecurity />
                <span className={cl.personalText}>
                    Devices:
                </span>
                <DevicesList />
            </span>
        </CSSTransition>
    );
}

export default VdbPersonal;