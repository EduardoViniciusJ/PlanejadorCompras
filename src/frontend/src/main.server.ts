import { registerLocaleData } from '@angular/common';
import pt from '@angular/common/locales/pt';
import { BootstrapContext, bootstrapApplication } from '@angular/platform-browser';

import { App } from './app/app';
import { config } from './app/app.config.server';

registerLocaleData(pt);

const bootstrap = (context: BootstrapContext) => bootstrapApplication(App, config, context);

export default bootstrap;
