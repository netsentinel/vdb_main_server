import cl from "./VdbHeader.module.css";
import { NavLink } from "react-router-dom";
import GlobalContext from '../../helpers/GlobalContext';

const VdbHeader: React.FC = () => {
    let accountPath = GlobalContext.currentUser
        ? "/personal" : "/auth";
    // let accountPathName = GlobalContext.currentUser
    //     ? "Account" ?? "Sign in";

    return (
        <header className={cl.header}>
            <nav className={cl.menuRow}>
                <NavLink to="/" className={cl.titleWrapper}>
                    <span className={cl.beta}>
                        v0.3-beta
                    </span>
                    <span className={cl.title}>
                        <strong>
                            Vdb
                        </strong>
                    </span>
                    <span className={cl.subtitle}>
                        .bruhcontent.ru
                    </span>
                </NavLink >
                <NavLink to="/download" className={cl.menuElement}>
                    Download
                </NavLink>
                <NavLink to={accountPath} className={cl.menuElement} >
                    Account
                </NavLink>
            </nav>
        </header>);
}

export default VdbHeader;