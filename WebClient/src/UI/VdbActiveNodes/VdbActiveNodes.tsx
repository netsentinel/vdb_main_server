import cl from "./VdbActiveNodes.module.css";
import { NavLink } from "react-router-dom";
import GlobalContext from '../../helpers/GlobalContext';
import hrefs from "../../config/hrefsList.json";
import { useState, useEffect } from 'react';
import { CSSTransition } from 'react-transition-group';
import ApiHelper from '../../helpers/ApiHelper';
import { IPublicNodeInfo } from '../../models/Connection/PublicNodeInfo';
import ActiveNodeCard from "../ActiveNodeCard/ActiveNodeCard";
import EnvHelper from '../../helpers/EnvHelper';

const VdbActiveNodes: React.FC = () => {
    const [transState, setTransState] = useState(false);
    const [nodes, setNodes] = useState<IPublicNodeInfo[]>();
    useEffect(() => {
        setTransState(true);
        ApiHelper.getNodesList().then(r => {
            if (!r) return;

            if (EnvHelper.isDebugMode() && (r!.length == 1)) {
                r = [
                    ...r,
                    ...JSON.parse(JSON.stringify(r)),
                    ...JSON.parse(JSON.stringify(r)),
                    ...JSON.parse(JSON.stringify(r)),
                    ...JSON.parse(JSON.stringify(r))
                ];
                r[0].isActive = true;
                r[0].clientsConnected = 150;
                r[3].isActive = true;
                r[3].clientsConnected = 1;
            }

            setNodes(r);
        });
    }, []);

    return (
        nodes
            ? <span className={cl.nodesWrapper}>
                {nodes.map(x => <ActiveNodeCard {...x} key={x.id} />)}
            </span>
            : <span className={cl.noNodesMsg}>There are no online nodes for now.</span>
    );
}

export default VdbActiveNodes;