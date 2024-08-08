import cl from "./AuthForm.module.css";
import { NavLink, useNavigate } from 'react-router-dom';
import GlobalContext from '../../helpers/GlobalContext';
import { CSSTransition } from 'react-transition-group';
import { Route, Routes } from "react-router-dom";
import VdbWelcome from '../VdbWelcome/VdbWelcome';
import { useState, useEffect, useMemo } from 'react';
import EnvHelper from '../../helpers/EnvHelper';
import AuthHelper from '../../helpers/AuthHelper';
import authSettings from "../../config/authSettings.json"
import RegistrationRequest from '../../models/Auth/RegistrationRequest';
import ValidationHelper from '../../helpers/ValidationHelper';
import eps from '../../config/endpoints.json';

const AuthForm: React.FC = () => {
    const navigate = useNavigate();
    const [transState, setTransState] = useState(false);
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [submitEnabled, setSubmitEnabled] = useState(true);
    const [errorMessage, setErrorMessage] = useState("");

    useEffect(() => {
        setTransState(true);
    }, []);

    useMemo(() => {
        AuthHelper.EnsureUserInContext().then(r => {
            if (r && GlobalContext.currentUser !== undefined) {
                navigate("/personal");
            }
        });
    }, []);

    const onSubmit = async () => {
        if (EnvHelper.isDebugMode())
            setErrorMessage("Test erorr message");

        let emailError = ValidationHelper.ValidateEmailAndGetError(email);
        if (emailError) {
            console.info("Email failed client-side validation.");
            setErrorMessage(emailError);
            setSubmitEnabled(true);
            return;
        }

        let passError = ValidationHelper.ValidatePasswordAndGetError(password);
        if (passError) {
            console.info("Password failed client-side validation.");
            setErrorMessage(passError);
            setSubmitEnabled(true);
            return;
        }

        if (!EnvHelper.isDebugMode()) {
            setSubmitEnabled(false);
            if (!EnvHelper.isDebugMode()) setTimeout(() => setSubmitEnabled(true), 5000);
        }
        setErrorMessage("");
        console.info(`Submitting: ${email}:${password}`);


        let result = false;
        try {
            result = await AuthHelper.PerformRegistrationAsync(new RegistrationRequest(email, password), true);
        } catch { }

        if (!result) {
            if (AuthHelper.lastStatus) {
                let status = AuthHelper.lastStatus;
                let selectedError: string;

                if (status === 200) {
                    console.info("Redirecting to personal...");
                    window.location.replace("/personal"); // not really needed ? no! Its needed to refresh links! in header, at least
                    return;
                }
                else if (status === 400)
                    selectedError = "Problem on client side. Please reload window.";
                else if (status === 401)
                    selectedError = "Wrong password.";
                else if (status === 404)
                    selectedError = "Server was unable to find existing account and refused to create a new one.";
                else if (status === 409)
                    selectedError = "Server has found existing account but refused to authorize it.";
                else if (status >= 500)
                    selectedError = "Problem on server side. Please try later.";
                else {
                    selectedError = "Unable to send data to server.";
                }

                setErrorMessage(selectedError + ` Error code: HTTP_${status}.`);
            }
            else {
                setErrorMessage("Unable to send data to server.")
            }
        }
        else {
            console.info("Redirecting to personal...");
            window.location.replace("/personal");
            return;
        }
        setSubmitEnabled(true);
    }


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
            <span className={cl.authWrapper}>
                <span className={cl.authHeader}>
                    <strong>Sign in</strong>&nbsp;or&nbsp;<strong>Sign up</strong>
                </span>
                <span className={cl.credentialsLabel}>Email</span>
                <input
                    onKeyUp={e => { if (e.key.toLowerCase() === "enter") onSubmit(); }}
                    onDragEnter={onSubmit}
                    type={"email"}
                    placeholder="Email"
                    value={email} onChange={(e) => setEmail(e.target.value)}
                    className={cl.credentialsInput} />
                <span className={cl.credentialsLabel}>Password</span>
                <input
                    onKeyUp={e => { if (e.key.toLowerCase() === "enter") onSubmit(); }}
                    type={"password"}
                    placeholder="Password"
                    value={password} onChange={(e) => setPassword(e.target.value)}
                    className={cl.credentialsInput} />
                <a className={cl.forgotPass} onClick={() => {navigate("/forgot-password")}} style={{cursor:"pointer"}}>
                    <span>I forgot it!</span>
                </a>
                <button
                    type="submit"
                    onClick={onSubmit}
                    className={cl.authSubmit}
                    disabled={!submitEnabled}>SUBMIT</button>
                {errorMessage
                    ? <span className={cl.errorWrapper}>
                        <span>{errorMessage}</span>
                    </span>
                    : <span />
                }
            </span>
        </CSSTransition>
    );
}

export default AuthForm;