import cl from "./VdbWelcome.module.css";
import { NavLink } from "react-router-dom";
import GlobalContext from '../../helpers/GlobalContext';
import { CSSTransition, TransitionGroup } from 'react-transition-group';
import { useState, useEffect } from 'react';
import VdbActiveNodes from "../VdbActiveNodes/VdbActiveNodes";



const VdbWelcome: React.FC = () => {
    const [fastTransSt, setFastTransSt] = useState(false);
    const [secureTransSt, setSecureTransSt] = useState(false);
    const [sourceTransSt, setSourceTransSt] = useState(false);
    const [descrTransSt, setDescrTransSt] = useState(false);
    const [othersTransSt, setOthersTransSt] = useState(false);

    useEffect(() => {
        setTimeout(() => setFastTransSt(true), 0);
        setTimeout(() => setSecureTransSt(true), 200);
        setTimeout(() => setSourceTransSt(true), 400);
        setTimeout(() => setDescrTransSt(true), 600);
        setTimeout(() => setOthersTransSt(true), 800);
    }, []);

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
        <span className={cl.welcomeWrapper}>
            <span className={cl.welcomeRow}>
                <strong>
                    <span className={cl.welcomWords}>
                        <CSSTransition in={fastTransSt} {...commonTransProp}>
                            <span className={cl.welcomeWord}>
                                Fast.
                            </span >
                        </CSSTransition>
                        <CSSTransition in={secureTransSt} {...commonTransProp}>
                            <span className={cl.welcomeWord}>
                                Secure.
                            </span>
                        </CSSTransition>
                        <CSSTransition in={sourceTransSt} {...commonTransProp}>
                            <span className={cl.welcomeWord}>
                                Open-source.
                            </span>
                        </CSSTransition>
                    </span>
                </strong>
                <span className={cl.welcomeDescription}>
                    <span className={cl.welcomWords}>
                        <CSSTransition in={descrTransSt} {...commonTransProp}>
                            <span className={cl.welcomeDecriptionPhrase}>
                                Our servers provide up to 10 Gbit/s channel.
                            </span >
                        </CSSTransition>
                        <CSSTransition in={descrTransSt} {...commonTransProp}>
                            <span className={cl.welcomeDecriptionPhrase}>
                                We are using TLSv1.3 for every of your connection
                                to the API. All your VPN traffic is encrypted with
                                modern 'ChaCha20' protocol.
                            </span>
                        </CSSTransition>
                        <CSSTransition in={descrTransSt} {...commonTransProp}>
                            <span className={cl.welcomeDecriptionPhrase}>
                                Additional goal is you to know which code you are running.
                                All sources are public.
                            </span>
                        </CSSTransition>
                    </span>
                </span>
            </span>
            <CSSTransition in={othersTransSt} {...commonTransProp}>
                <span className={cl.currentStatusPhrase}>
                    Current status:
                </span>
            </CSSTransition>
            <CSSTransition in={othersTransSt} {...commonTransProp}>
                <span className={cl.nodesList}>
                    <VdbActiveNodes />
                </span>
            </CSSTransition>
            <CSSTransition in={othersTransSt} {...commonTransProp}>
                <span className={cl.currentStatusPhrase}>
                    About:
                </span>
            </CSSTransition>
            <CSSTransition in={othersTransSt} {...commonTransProp}>
                {/* https://medium.com/@ist.stevkovski/is-it-front-end-or-front-end-or-frontend-3ae717cae4aa */}
                <span className={cl.aboutText}>
                    <p style={{ marginTop: ".75rem" }}>
                        Vdb (VPN Developed Badly) is a vpn-service originally developed by a single&nbsp;
                        <a href="https://t.me/luminodiode" className={cl.meLink}>person</a> as a bachelor's degree coursework.
                        However, it is truly operational - you can create account and then a tunnel to the one of our servers.
                        The service is using <b>ReactTS</b> as a front end, <b>ASP WebAPI</b> as a back end, <b>WPF</b> and <b>Inno Setup</b> for a desktop app (electronJS is planned).
                        All the back-end services are containerized using <b>Docker</b> and launched with compose.
                    </p>
                    <p style={{ marginTop: ".75rem" }}>
                        The <b>NGINX</b>es all across the app are configured to use only strong ciphers (according to the SSL Labs classification).
                        Some of the backend connections is configured to be kept alive using nginx <b>upstream keep_alive</b> directive to avoid
                        unnesessary handshakes.
                        It is common for the server to <b>cache the response</b> for anonymous endpoints for few minutes.
                    </p>
                    <p style={{ marginTop: ".75rem" }}>
                        The <b>Wireguard</b> protocol was chosen as a primary because of being fastest with smallest service-traffic amount across others.
                    </p>
                    <p style={{ marginTop: ".75rem" }}>
                        The nodes itself are the highly separated applications. They were designed to be usable for <b>both personal and enterprise purposes</b>.
                        Using environment variables you can make just a common WebAPI-managed wireguard server. See open-source for more info.
                    </p>
                </span>
            </CSSTransition>
            <CSSTransition in={othersTransSt} {...commonTransProp}>
                <span className={cl.currentStatusPhrase}>
                    Open-source:
                </span>
            </CSSTransition>
            <CSSTransition in={othersTransSt} {...commonTransProp}>
                <span className={cl.aboutText}>
                    <p style={{ marginTop: ".75rem" }}>
                        VPN Servers
                    </p>
                    <p style={{ marginLeft: "2.75rem" }}>
                        
                        <a
                            href="https://github.com/LuminoDiode/rest2wireguard"
                            className={cl.myLink}>
                            github.com/LuminoDiode/rest2wireguard
                        </a>
                    </p>
                    <p style={{ marginTop: ".75rem" }}>
                        Main Server
                    </p>
                    <p style={{ marginLeft: "2.75rem" }}>
                        
                        <a 
                            href="https://github.com/LuminoDiode/vdb_main_server"
                            className={cl.myLink}>
                            github.com/LuminoDiode/vdb_main_server
                        </a>
                    </p>
                    <p style={{ marginTop: ".75rem" }}>
                        Web Client
                    </p>
                    <p style={{ marginLeft: "2.75rem" }}>
                        
                        <a 
                            href="https://github.com/LuminoDiode/vdb_web_client"
                            className={cl.myLink}>
                            github.com/LuminoDiode/vdb_web_client
                        </a>
                    </p>
                    <p style={{ marginTop: ".75rem" }}>
                        Dekstop App
                    </p>
                    <p style={{ marginLeft: "2.75rem" }}>
                        
                        <a 
                            href="https://github.com/LuminoDiode/vdb_desktop_client"
                            className={cl.myLink}>
                            github.com/LuminoDiode/vdb_desktop_client
                        </a>
                    </p>
                </span>
            </CSSTransition >
            <hr style={{ marginTop: "6rem", color:"lightgray"}}></hr>
            <span className={cl.outerText}>R.Ryzhov @2023</span>
        </span >
    );
}

export default VdbWelcome;