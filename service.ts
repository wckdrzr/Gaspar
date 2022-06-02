//**
//** This file was written by a tool
//**
//** It contains all controllers in:
//**     /Users/ben/WCKDRZR/data-watchdog/services/Tag/src/Tag/Controllers/*.cs
//**     only if they are attributed: [FrontEnd]
//**
//** full configuration in: /Users/ben/WCKDRZR/csharp-walker/csharp-models-to-typescript.config.json
//**


import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { OntologyEntry, SystemTag } from "./common";

// /Users/ben/WCKDRZR/data-watchdog/services/Tag/src/Tag/Controllers/TagsController.cs

export class TagService {

    constructor(private http: HttpClient) {}

    all(): Observable <OntologyEntry[]> {
        return this.http.get<OntologyEntry[]> (`/api/tag/Tag`);
    }
    tagById(id: string): Observable <OntologyEntry> {
        return this.http.get<OntologyEntry> (`/api/tag/Tag/${id}`);
    }
    changeSystemTag(id: string, systemTag: SystemTag): Observable <OntologyEntry> {
        return this.http.put<OntologyEntry> (`/api/tag/Tag/${id}/${systemTag}`, null);
    }
    /**
     * @deprecated; this method is broken: Parameter systemTag not declared in route
     */
    forSystemTag(systemTag: SystemTag) {}
    addTag(body: OntologyEntry): Observable <OntologyEntry> {
        return this.http.post<OntologyEntry> (`/api/tag/Tag`, body);
    }
    editTag(id: string, body: OntologyEntry): Observable <OntologyEntry> {
        return this.http.put<OntologyEntry> (`/api/tag/Tag/${id}`, body);
    }
    deleteTag(id: string): Observable <string | null> {
        return this.http.delete<string | null> (`/api/tag/Tag/${id}`);
    }
    synchronizeCollibraTags(body: Credentials): Observable <OntologyEntry[]> {
        return this.http.post<OntologyEntry[]> (`/api/tag/Tag/Collibra/Tags/Synchronize`, body);
    }
}