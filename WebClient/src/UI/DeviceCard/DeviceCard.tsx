import { useState } from "react";
import cl from "./DeviceCard.module.css";
import UserApiHelper from "src/helpers/UserApiHelper";
import IUserDevice from '../../models/Device/IUserDevice';

const DeviceCard: React.FC<IUserDevice> = (props) => {
    const [isButtonDisabled, setButtonDisabled] = useState(false);
    const [errorMsg, setErrorMsg] = useState("");
    const [commonMsg, setCommonMsg] = useState("");

    const id = props.id;
    const pk = props.wireguardPublicKey;
    const lastNode = props.lastConnectedNodeId;
    const lastSeen = props.lastSeenUtc;

    const onDelete = async () => {
        console.log("Disabling remove button...");
        setButtonDisabled(true);

        let deleted = await new UserApiHelper().removeUserDevice(pk)

        if (deleted) {
            console.info("Device was deleted.");
            setCommonMsg("Removed successfully.");
            return;
        } else {
            console.info("Unable to remove device.");
            setErrorMsg("Unable to remove device.");
        }

        console.log("Enabling remove button...");
        setTimeout(() => setButtonDisabled(false), 5000);
    }

    return (
        <span className={[cl.cardWrapper].join(' ')} key={id}>
            <span className={cl.col}>
                <span className={cl.pkSpan}>
                    <strong>Key:</strong>&nbsp;{pk ?? "unknown"}
                </span>
                <span>
                    <strong>Last seen:</strong>&nbsp;{lastSeen?.split('.')[0].split('T').join(' ') ?? "never"}
                </span>
                <span>
                    <strong>Last node:</strong>&nbsp;{lastNode ?? "none"}
                </span>
            </span>
            <span className={cl.removeWrapper}>
                <button className={cl.removeButton} onClick={onDelete} disabled={isButtonDisabled}>
                    DISCONNECT AND REMOVE
                </button>
                {errorMsg
                    ? <span className={cl.errorMsg}>{errorMsg}</span>
                    : <span />}
                {commonMsg
                    ? <span className={cl.commonMsg}>{commonMsg}</span>
                    : <span />}
            </span>
        </span>);
}

export default DeviceCard;