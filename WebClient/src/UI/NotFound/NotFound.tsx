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

const NotFound: React.FC = () => {

    return (
        <span style={{fontFamily:"monospace", display:"flex",alignSelf:"center", fontSize:"1rem", margin:"1rem"}}>
            404! Page not found!
        </span>
       );
}

export default NotFound;