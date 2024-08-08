import { useState, useEffect, useMemo } from "react";
import cl from "./DevicesList.module.css";
import EnvHelper from "src/helpers/EnvHelper";
import IUserDevice from '../../models/Device/IUserDevice';
import UserApiHelper from '../../helpers/UserApiHelper';
import DeviceCard from '../DeviceCard/DeviceCard';
import { CSSTransition } from 'react-transition-group';
import ApiHelper from '../../helpers/ApiHelper';
import GlobalContext from '../../helpers/GlobalContext';
import IUserInfoFromJwt from '../../models/Auth/IUserInfoFromJwt';

const DevicesList: React.FC = () => {
    const [devices, setDevices] = useState<IUserDevice[]>();

    useMemo(() => {
        var helper = new UserApiHelper();
        if (!devices) {
            helper.getUserDevices().then(
                d => {
                    if (d && d.length > 0) {
                        setDevices(d);
                        return;
                    }
                    if ((!d || d.length < 1) && EnvHelper.isDebugMode()) { // add some entities for testing
                        setDevices([
                            {
                                id: 1, userId: 1, lastConnectedNodeId: 1, lastSeenUtc: "111",
                                wireguardPublicKey: "Kq0LygX5ESfSpIDQO0k4dGSCnOAIZlJDPFacBeOBMCE="
                            },
                            {
                                id: 2, userId: 1, lastConnectedNodeId: 1, lastSeenUtc: "111",
                                wireguardPublicKey: "HwHqB2DgFzxqkyxyF3vN4bA3DXD1lBKrYvm4Dr4oiBU="
                            },
                            {
                                id: 3, userId: 1, lastConnectedNodeId: 1, lastSeenUtc: "111",
                                wireguardPublicKey: "Ugm+Dvja3Pvf9qNag4Muy+xsQjELnenTadpj6CmTuWQ="
                            },
                        ])
                    }
                }
            );
        }
    }, []);

    const [transState, setTransState] = useState(false);
    useEffect(() => {
        setTransState(true);
    }, []);

    const transitionClasses = {
        enterActive: cl.welcomeTransitionEnter,
        enterDone: cl.welcomeTransitionEnterActive,
        exitActive: cl.welcomeTransitionExit,
        exitDone: cl.welcomeTransitionExitActive,
    }
    const commonTransProp = {
        timeout: 200,
        classNames: transitionClasses
    }

    return (
        <CSSTransition in={transState} {...commonTransProp}>
            <span className={[cl.deviceListWrapper].join(' ')}>
                {(devices && devices.length > 0)
                    ? devices.map(x => <DeviceCard {...x} key={x.id} />)
                    : <span className={[cl.noNodesMsg].join(' ')}>
                        You have no devices.
                    </span>}
            </span>
        </CSSTransition>);
}

export default DevicesList;