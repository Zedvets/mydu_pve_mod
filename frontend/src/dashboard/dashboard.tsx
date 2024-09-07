import {Outlet, useNavigate} from "react-router-dom";
import React from "react";
import {AppProvider, DashboardLayout, Navigation, Router} from "@toolpad/core";
import {createTheme} from "@mui/material";
import {Bolt, Checklist, GridOn, Inventory, LensBlur, Reorder, Slideshow, ViewInAr} from "@mui/icons-material";

const NAVIGATION: Navigation = [
    {
        kind: 'header',
        title: 'Spawner',
    },
    {
        segment: 'prefab',
        title: 'Prefabs',
        icon: <Inventory/>,
    },
    {
        segment: 'dashboard',
        title: 'Scripts',
        icon: <Slideshow/>,
    },
    {
        kind: 'divider',
    },
    {
        kind: 'header',
        title: 'Sectors',
    },
    {
        segment: 'sector-encounters',
        title: 'Sector Definitions',
        icon: <GridOn/>,
    },
    {
        segment: 'sector-instances',
        title: 'Sector Instances',
        icon: <LensBlur/>,
    },
    {
        segment: 'sector-3d',
        title: 'Sector 3D View',
        icon: <ViewInAr/>,
    },
    {
        kind: 'divider',
    },
    {
        kind: 'header',
        title: 'Events',
    },
    {
        segment: 'event-handlers',
        title: 'Event Handlers',
        icon: <Bolt/>,
    },
    {
        kind: 'divider',
    },
    {
        kind: 'header',
        title: 'Others',
    },
    {
        segment: 'event-handlers',
        title: 'Features',
        icon: <Checklist/>,
    },
    {
        segment: 'event-handlers',
        title: 'Task Queue',
        icon: <Reorder/>,
    },
];

const theme = createTheme({
    cssVariables: {
        colorSchemeSelector: 'data-toolpad-color-scheme',
    },
    colorSchemes: {light: true, dark: true},
    breakpoints: {
        values: {
            xs: 0,
            sm: 600,
            md: 600,
            lg: 1200,
            xl: 1536,
        },
    },
});

const Dashboard = (props: any) => {

    const [pathname, setPathname] = React.useState('/prefab');
    const navigate = useNavigate();

    const router = React.useMemo<Router>(() => {
        return {
            pathname,
            searchParams: new URLSearchParams(),
            navigate: (path) => {
                //window.history.pushState({}, '', path);
                // window.location.href = path.toString();
                navigate(path);
                setPathname(String(path));
            },
        };
    }, [navigate]);

    return <AppProvider
        branding={{
            title: "Dynamic Encounters"
        }}
        navigation={NAVIGATION}
        router={router}
        theme={theme}
    >
        <DashboardLayout>
            <Outlet />
        </DashboardLayout>
    </AppProvider>
};

export default Dashboard;