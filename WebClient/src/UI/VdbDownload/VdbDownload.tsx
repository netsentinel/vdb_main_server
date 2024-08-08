import cl from "./VdbDownload.module.css";
import { NavLink } from "react-router-dom";
import GlobalContext from '../../helpers/GlobalContext';
import hrefs from "../../config/hrefsList.json";
import { useState, useEffect } from 'react';
import { CSSTransition } from 'react-transition-group';
import fixs from "../AuthForm/AuthForm.module.css";

const VdbDownload: React.FC = () => {
    const [transState, setTransState] = useState(false);
    const [releaseTransState, setReleaseTransState] = useState(false);
    const [wgTransState, setWgTransState] = useState(false);
    //const [dotnetTransState, setDotnetTransState] = useState(false);
    useEffect(() => {
        setTimeout(() => setTransState(true), 0);
        setTimeout(() => setReleaseTransState(true), 200);
        setTimeout(() => setWgTransState(true), 300);
        //setTimeout(() => setDotnetTransState(true), 400);
    }, []);


    const downloadClient = () => {
        window.open(GlobalContext.latestVersionHref ?? hrefs.installer, '_blank', 'noreferrer')
    }
    const goToVirusTotal = () => {
        window.open(hrefs.virusTotal, '_blank', 'noreferrer')
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
            <span className={cl.downloadWrapper}>
                <span className={cl.textSpan}>
                    We are currently providing only windows x64 application
                    with .NET8 runtime and wireguard required - both embedded into the installer.
                </span>
                <CSSTransition in={releaseTransState} {...commonTransProp}>
                    <button onClick={downloadClient} className={cl.goToReleaseButton}>
                        DOWNLOAD INSTALLER
                    </button>
                </CSSTransition>
                <CSSTransition in={wgTransState} {...commonTransProp}>
                    <button onClick={goToVirusTotal} className={cl.goToReleaseButton}>
                        VirusTotal
                    </button>
                </CSSTransition>
                {/* <span className={fixs.authHeader} style={{opacity:"0%"}}>this el is fixing layout!</span> */}
            </span>
        </CSSTransition>
    );
}

export default VdbDownload;