import cl from "./VdbMain.module.css";
import { NavLink } from "react-router-dom";
import GlobalContext from '../../helpers/GlobalContext';
import { CSSTransition } from 'react-transition-group';
import { Route, Routes } from "react-router-dom";
import VdbWelcome from '../VdbWelcome/VdbWelcome';
import AuthForm from "../AuthForm/AuthForm";
import VdbDownload from '../VdbDownload/VdbDownload';
import VdbPersonal from '../VdbPersonal/VdbPersonal';
import NotFound from "../NotFound/NotFound";
import eps from '../../config/endpoints.json'
import RecoveryPassword from "../RecoveryPassword/RecoveryPassword";
import ForgotPassword from '../ForgotPassword/ForgotPassword';

const VdbMain: React.FC = () => {

    return (
        <Routes>
            <Route path="/" element={<VdbWelcome />} />
            <Route path="/download" element={<VdbDownload />} />
            <Route path="/auth" element={<AuthForm />} />
            <Route path="/personal" element={<VdbPersonal />} />
            <Route path="/forgot-password" element={<ForgotPassword/>}/>
            <Route path={eps.fontend.passwordRecovery+"/*"} element={<RecoveryPassword/>} />
            <Route path="/*" element={<NotFound/>} />
        </Routes>
    );
}

export default VdbMain;