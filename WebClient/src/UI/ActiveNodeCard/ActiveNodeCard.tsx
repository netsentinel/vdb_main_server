import cl from "./ActiveNodeCard.module.css";
import { NavLink } from "react-router-dom";
import GlobalContext from '../../helpers/GlobalContext';
import hrefs from "../../config/hrefsList.json";
import { useState, useEffect } from 'react';
import { CSSTransition } from 'react-transition-group';
import ApiHelper from '../../helpers/ApiHelper';
import { IPublicNodeInfo } from "src/models/Connection/PublicNodeInfo";
import LocationNames from "../../config/locationsPseudonyms.json";
import EnvHelper from '../../helpers/EnvHelper';

const ActiveNodeCard: React.FC<IPublicNodeInfo> = (props) => {
    const [transState, setTransState] = useState(false);
    useEffect(() => {
        setTransState(true);
    }, []);

    let shortenName = props.name.substring(1 - 1, 3).toLowerCase();
    let locationName: string;
    if (shortenName === "ams") locationName = LocationNames.Ams; else
    if (shortenName === "frk") locationName = LocationNames.Frk; else
    if (shortenName === "hkg") locationName = LocationNames.Hkg; else
    if (shortenName === "stm") locationName = LocationNames.Stm; else
    if (shortenName === "vna") locationName = LocationNames.Vna; else
    if (shortenName === "ral") locationName = LocationNames.Ral; else
    locationName = shortenName;

    if (EnvHelper.isDebugMode()) {
        console.log(`${shortenName} expanded to ${locationName}`);
    }


    function capitalize(s: string) {
        return s.charAt(0).toUpperCase() + s.slice(1);
    }

    return (
        <span className={cl.cardWrapper}>
            <span className={props.isActive ? cl.cardActive : cl.cardInactive}>
                <span className={cl.nodeName}><strong style={{ width: "100%" }}>{capitalize(props.name)}</strong></span>
                <span className={cl.cardDescr}>
                    {locationName}, {props.ipAddress}, clients: {props.clientsConnected}
                </span>
            </span>
        </span>
    );
}

export default ActiveNodeCard;